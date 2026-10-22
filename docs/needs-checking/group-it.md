# Needs checking — Group IT

> One group of [the register](../NEEDS-CHECKING.md), in its own file since 2026-10-22 so that it can be read
> alone. **The register's rules, and every group's place in it, are on that page.** A new row
> for this group goes in this file. [`tools/needs-checking-register.py`](../../tools/needs-checking-register.py)
> reads it back into the register for every tool that reads it, so a row here is seen
> exactly as it was seen there.

## Group IT - a selection that refuses a mode it does not know, takes the whole model when no view is named, and knows a connector by its name: `SELECT_BY_PARAMETER_VALUE` v3, `SELECT_BY_CATEGORIES` v3, `SELECT_OPENINGS` v3, the binder's absent View and category aliases (2026-10-22)

From rows [5b-1319, 5b-1320 and 5b-1321](../fragment-issues/section-5b-rows-401-425.md). All three were seen on
Revit 2020 (session 78736) on 2026-10-22, in a chat building a nested floor-drain family: `matchMode=exact`
widened a selection that fed a delete to two reference planes, the plumbing fixtures could not be asked for
without naming a view, and a connector could only be named `OST_ConnectorElem`. None lost anything - the wider
selection was read before the delete. All three fixed the same day with no Revit:

| What | Where | What changed |
|---|---|---|
| `SELECT_BY_PARAMETER_VALUE` v3 | [`select-by-parameter-value`](../../brain/fragments/select-by-parameter-value/fragment.yaml) | READ. A `matchMode` the code does not know is refused before anything is scanned, naming contains, equals, begins, ends, notcontains and notequals; `exact` and `is` are read as equals. Version 2 fell through to contains in silence (5b-1319) |
| `SELECT_BY_CATEGORIES` v3, `SELECT_OPENINGS` v3 | [`select-by-categories`](../../brain/fragments/select-by-categories/fragment.yaml), [`select-openings`](../../brain/fragments/select-openings/fragment.yaml) | READ. `inViewOnly` is marked `optional: true`: left out, or given empty, it binds null - the whole model, which the code always read null as; a view named still narrows. Their code is unchanged, so each `proof-held:` says why the card is DRAFT (5b-1320) |
| The binder, an absent View | [`HeronBindingNote.cs`](../../revit/Heron.Revit.Addin/HeronBindingNote.cs), [`RevitFragment.cs`](../../revit/Heron.Revit.Addin/RevitFragment.cs), [`heron_fragment.py`](../../brain/heron_fragment.py) | `AbsentValue` binds an absent View marked optional to null; `EmptyMeansAbsent` reads a value given empty as absent, asked before the text is converted, wherever the card says what absent means. `CREATE_LINE`'s optional view, row 5b-227's case, binds null through the same rule; the Python mirror says the same, so the server no longer hands the call back for want of a view (5b-1320) |
| The binder, category names | [`RevitFragment.cs`](../../revit/Heron.Revit.Addin/RevitFragment.cs) | `CategoryAliases`: "Connector Elements", "Connectors" and "Connector" bind `OST_ConnectorElem`, whatever the capitals - asked after the name Revit shows and before the "OST_" guess, so every name that bound before binds the same value. No other name was added: nothing measured asks for one (5b-1321) |

**What is done and what is not.** [`tests/test_binding_note.py`](../../tests/test_binding_note.py) builds its host
and proves every absent and empty case - 9 of its checks were red on main's code, including the one that
held an optional View refused by name; [`tests/test_category_by_name.py`](../../tests/test_category_by_name.py)
reads the table and its place in the order - 8 new checks, red on main's code. **Nothing here has run in
Revit.** The three cards are DRAFT, and a DRAFT read still runs and says DRAFT, so none of the three tools
is lost until the rows below are signed.

**What to restart.** IT0 to IT3 need nothing restarted - a fragment's C# is read live. IT4 to IT10 need the
add-in **built and deployed for Revit 2020 and Revit restarted** (the binder is in the add-in), and the
**Claude app restarted** (the server's mirror in `heron_fragment.py`; with the old one the call is handed
back for want of a view before Revit is asked, which reads like the fault unfixed).

**The models.** For IT0 to IT3 and IT9 to IT10, the floor-drain family of 2026-10-22 as it stands, or a
new family from Revit 2020's *Metric Generic Model.rft* with two reference planes named *Outlet Port* and
*Outlet Port Bore* (`CREATE_REFERENCE_PLANES`) and one connector on a face (`ADD_FAMILY_CONNECTOR`). For
IT4 to IT8, a project: Project2 on Revit 2020, as the 2026-10-18 proof of `SELECT_BY_CATEGORIES` used, with
a plan that draws fewer of a category than the model holds, and an opening or two. Run every row on
**Revit 2020**, then IT4 and IT9 again on the newest Revit on the PC.

| # | Run | Look for |
|---|---|---|
| **IT0** | *Negative FIRST.* `SELECT_BY_PARAMETER_VALUE` with `parameterName=Name matchText=Outlet Port matchMode=exactly categories=Reference Planes` | Refused *"matchMode 'exactly' is not a way of matching this knows, so NOTHING was scanned. Use contains, equals (or exact, is), begins, ends, notcontains or notequals"*; no `elements` left behind, so a next step with `expect_from` refuses. Never a count |
| **IT1** | *The fault's own case.* The same with `matchMode=exact` | `elements` 1 - *Outlet Port* alone - and the finding *"1 of N scanned element(s) match Name equals 'Outlet Port'"*. Never *Outlet Port Bore* |
| **IT2** | `matchMode=is`, then `matchMode=EXACT` | The same answer as IT1, both times |
| **IT3** | `matchMode=contains` | 2 - both planes, asked for by name; the finding reads `contains` |
| **IT4** | *5b-1320's own case, add-in rebuilt.* `SELECT_BY_CATEGORIES` with `categories=Plumbing Fixtures includeLinks=false` and `inViewOnly` LEFT OUT, in the project | Runs. `found` is the whole model's count - compare a schedule of Plumbing Fixtures - and the binding note reads *"inViewOnly not given - null (optional)"* |
| **IT5** | The same with `inViewOnly=` and nothing after it | The same count; the note reads *"inViewOnly given empty - null (optional)"* |
| **IT6** | The same with `inViewOnly=FloorPlan: Level 1` - a plan that draws only some of them | Fewer than IT4: a view named still narrows, as version 2's proof measured |
| **IT7** | *Negative.* `inViewOnly=No Such View` | Refused *"No view called "No Such View" ..."* - optional lets the view be left out, never lets a wrong name mean the whole model |
| **IT8** | `SELECT_OPENINGS` with `inViewOnly` left out, then empty, in the project; then `CREATE_LINE` through `revit_change` with no `view`, `asDetail=false` and one pair of points (Discard it at Revit's question), and once more with `asDetail=true` and no `view` | Openings: the whole model's count both times, the note as IT4 and IT5. The model line: one drawn, `refused` empty. The detail line: nothing drawn, `refused` reading *"a detail line has to be drawn in a view, and no view was given"* - the code's own words, reached for the first time |
| **IT9** | *5b-1321's own case, add-in rebuilt.* In the family: `SELECT_BY_CATEGORIES` with `categories=Connector Elements includeLinks=false`, no view; then `categories=Connectors`; then `categories=connector` | Each binds - never *"is not a category Revit knows"* - and `found` is the family's connector count, each time |
| **IT10** | *Negative.* `categories=Connector Thing` | Refused *""Connector Thing" is not a category Revit knows. Type what Visibility/Graphics shows ..."* - the table adds three words and nothing else |

**Then sign** `select-by-parameter-value` version 3 on IT1 with IT0's negative, after IT2 and IT3 have
passed; `select-by-categories` version 3 on IT4 with IT7's negative, after IT5 and IT6; `select-openings`
version 3 on IT8's two runs with IT7's shape as its negative. Row 5b-1321 closes on IT9 and IT10, and row
5b-227's View case on IT8's line - both are the binder's, with no card to sign. A row that fails reopens its
row; nothing in a row changes to make it pass.
