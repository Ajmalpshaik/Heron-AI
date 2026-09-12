# The twenty-fourth session, 2026-09-02 — eight more, and two capabilities that already existed

> **Archived session note.** This is a record of work that is finished. It was moved out of
> [`HANDOVER.md`](../HANDOVER.md) on 2026-09-12 so that file could go back to being the short
> live entry point it is supposed to be. Nothing here is specification: where it disagrees with
> [DECISIONS.md](../DECISIONS.md), the [Golden Rules](../14-golden-rules.md) or the
> [Constitution](../../HERON_CONSTITUTION.md), **those win**.

---


**What it did:** took the library from **170 to 178**. Same container, same limitation. `A9` now names
**forty**.

| | |
|---|---|
| `COPY_VIEW_FILTERS` | *"copy the view filters from this view to the other views"* was answering `REPORT_VIEW_FILTERS` — a read, to a request to change twelve drawings. **Adding a filter is not copying it**: `AddFilter` carries EMPTY overrides, the look is a second object and the on/off state a third. Copy one of the three and twelve views hold the right filters and look identical, which reads as the job not having run |
| `REMAP_LINE_STYLES` | *"change all the old line styles to the new ones"* was answering `CREATE_FLOOR`. **Lines live where a selection cannot reach** — inside sketches, and inside filled region borders, which are not curve elements at all. And Revit gives a setter for a region's border style and **no getter**, so there is no honest way to change only the offending ones; the fragment says which of the three choices that leaves rather than pretending |
| `SET_VIEW_WORKSET_VISIBILITY` | *"turn off this workset in this view"* was answering `SET_CATEGORY_VISIBILITY` — a category is what a thing IS, a workset is who OWNS it. **A workset has three states in a view, not two.** Turning the wanted one on without pushing the rest to Hidden leaves them on Use Global Setting, which is usually visible: the view shows everything and the call looks like a no-op |
| `CREATE_SHEET_LIST` | *"make a sheet list schedule"* was answering `FIND_SCHEDULES`. **A different API call, not an option on the schedule maker** — sheets are not model elements, so the category route cannot produce a drawing index at all. A field the list cannot carry is named with the real ones, because a silently short index prints on the cover and nobody reading it can tell a column was asked for |
| `REPORT_GLOBAL_PARAMETERS` | *"show me the global parameters"* was answering `READ_MODEL_WARNINGS`. **A global is not a project parameter**: one value for the whole model that drives geometry, against a column with one value each. The interesting part is which DIMENSIONS it drives — that is the only answer to *"why did that wall shift when I changed a number"*. And a document that CANNOT hold them is a different answer from one that has none, which is why that is asked first |
| `CREATE_LINE` | *"draw a detail line here"* was answering `CREATE_DRAFTING_VIEW`. **Detail or model is the decision, not a detail** — one lives in a single view, the other turns up on every drawing — so there is no default and the caller says which. The short-curve limit is read from the application rather than typed, because it belongs to the installation |
| `ASSIGN_SCOPE_BOX_TO_VIEW` | *"set the scope box on these views"* was answering `SET_VIEW_SECTION_BOX`. Pairs with the impossibility already recorded here: **a scope box cannot be CREATED from code on any release** — but assigning one that exists is a parameter write, and that is where the repeated work is. A model with none says so, and says why |
| `SET_VIEW_CROP_TO_SHAPE` | *"crop the view to follow the room shape"* was answering `SET_VIEW_CROP`, which sets a BOX and can only ever be a rectangle. Revit demands three things of the loop and **gives the same unhelpful error for all three**; the curves are chained here rather than trusted in arrival order, and the worst gap bridged is reported so a shape that never closed does not pass as one that did |

**Two sentences were answered by a routing row instead of a fragment, and that is the batch's real
finding.** *"Grey out the linked model"* was routing to `COPY_FROM_LINK`. The instinct was to build a
link-override fragment — and reading `OVERRIDE_GRAPHICS_IN_VIEW` first showed it already does the job:
**a link is ONE element in the host document**, so a single override on it covers everything inside. It
needed the sentence declared, not a new fragment. *"Make a new duct system type"* is the same shape:
`DUPLICATE_TYPE` is how a system type is made in Revit.

**What that is worth remembering as:** the twenty-third session built a duplicate because it read the
brain's #1 answer and stopped. This one read the top five for every sentence **and then opened the
purpose of each near neighbour**, and that second step is what caught these two — the ranking had them
at #1 and #2 both times. Reading the list is not the same as reading the fragments.

**A gap this batch measured and deliberately did not fill:** *"make this pipe look the same as that
one"* still answers `CREATE_PIPE`. `OVERRIDE_GRAPHICS_IN_VIEW` takes the settings already built, and
nothing in the library READS an element's existing overrides — `View.GetElementOverrides` exists on all
three releases checked. That is one small fragment, and it belongs to the next batch rather than to a
sentence declared on something that half-answers it.

**The checker caught two orphans in this batch's own work.** `COPY_VIEW_FILTERS` and
`ASSIGN_SCOPE_BOX_TO_VIEW` both named a list of views as a need without marking it `source: request`, so
`check-gaps` correctly reported two actions nothing could feed. Worth noting because both were written
by hand from a template that had it right — the error was mine and the tool found it in seconds.

**And the API check earned its keep a fourth time, in a new shape.** `GlobalParameter.IsValidDataType`
is present on 2020 and **gone by 2024**. The three it caught before were members missing from the OLD
end; this one is missing from the NEW end, which is the reverse and is worse, because a guard written
against the release in front of you looks careful and turns every newer build red.

---
