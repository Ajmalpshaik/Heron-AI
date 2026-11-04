# Needs checking — Group LM

> One group of [the register](../NEEDS-CHECKING.md), in its own file so that it can be read alone. **The
> register's rules, and every group's place in it, are on that page.** A new row for this group goes in
> this file. [`tools/needs-checking-register.py`](../../tools/needs-checking-register.py) reads it back
> into the register for every tool that reads it, so a row here is seen exactly as it was seen there.

## Group LM - a view's work plane set to a named level, reference plane or grid, shown or hidden, and read: `SET_VIEW_WORK_PLANE`, `REPORT_VIEW_WORK_PLANE` (2026-11-02)

Asked for in the modeller's words: *"set the work plane to Level 2"*, *"set the work plane to reference plane
Ceiling Datum"*, *"show the work plane in this view"*, *"hide the work plane"* and *"which work plane is this view
on"* - Revit's Architecture > Work Plane > Set and Show. No card set, showed or read a view's work plane: a grep of
every fragment's C# that day found `View.SketchPlane`, `SketchPlane.Create` and `View.ShowActiveWorkPlane` /
`HideActiveWorkPlane` in none, and the questions reached writes without the host's question flag
([row 5b-1392](../fragment-issues/section-5b-rows-401-425.md)). Checked by the duplicate checker's method:
`CREATE_PROJECT_REFERENCE_PLANES` draws reference planes and sets no work plane, `SET_VIEW_PROPERTIES` sets what the
Properties palette holds and the work plane is not in it, and `SET_FAMILY_SETTINGS` sets a family's Work
Plane-Based tick - look-alikes, not duplicates. The question was first written into the view read that already
existed, `REPORT_VIEW_SETTINGS`, then DRAFT on this branch; the same day #790 proved that card on Revit 2020
against code without it, so the read moved to a card of its own, `REPORT_VIEW_WORK_PLANE`, rather than make that
proof stale. `REPORT_VIEW_SETTINGS` keeps its proved code and its rows in [Group EC](group-ec.md) (EC14).

| What | Where | What it does |
|---|---|---|
| `SET_VIEW_WORK_PLANE` (MODIFY) | [`brain/fragments/set-view-work-plane`](../../brain/fragments/set-view-work-plane/fragment.yaml) | Sets the work plane of **one view** - the view named, or **the view in front** - to a **level, a named reference plane or a grid named by the modeller, never guessed**: none named, or a name nothing has, changes nothing and lists the levels, named reference planes and grids; a name two kinds share is refused until the kind is said ("reference plane Ceiling Datum"). `display` **show** or **hide** turns Show Work Plane on or off. Plans, sections, elevations, detail and 3D views only - **a sheet, a schedule, a legend, a drafting view and a view template are refused by name**; an arc grid is refused. A plane **already** the view's work plane is left alone and said so. The work plane is **read before and after** (`before`, `after`, `changed`, `report`, `refused`) and must read back as the one made, lying on the datum, or the whole call rolls back. **Revit has no member that reads whether a work plane is shown**, so show and hide are done and said, never claimed as read back. Nothing is deleted |
| `REPORT_VIEW_WORK_PLANE` (READ) | [`brain/fragments/report-view-work-plane`](../../brain/fragments/report-view-work-plane/fragment.yaml) | `workPlanes`: for each view named - **the view in front when none is** - the work plane as the Work Plane dialog's Current line names it (*Level : Level 2*, *Reference Plane : Ceiling Datum*, *Grid : B*) and where it lies in millimetres, counted in `withPlane`; a sheet or schedule is named in `findings` as having **"no work plane"** and counted in `noPlane`, never given one. Declares *"which work plane is this view on"* and *"what is the work plane of this view"*. Opens no transaction |

**What is done and what is not.** Both compile for all eight releases, 2020 to 2027
(`tools/check-fragments-compile.py`, 2026-11-02, a Linux container - `REPORT_VIEW_WORK_PLANE` again after the
move). Read off the shipped assemblies with
`tools/api-surface --members`: `SketchPlane.Create(Document, ElementId)`, `(Document, Reference)` and
`(Document, Plane)`, `View.SketchPlane` get and set, `View.ShowActiveWorkPlane` and `HideActiveWorkPlane`,
`SketchPlane.GetPlane` and `GetPlaneReference`, `ReferencePlane.GetReference`, `Document.GetElement(Reference)` -
the same 2020 to 2027; `Grid` has no `GetPlaneReference`, so a grid's plane is made from its id. **Nothing here has
run in Revit.** Both cards are DRAFT. **Only Revit can settle** whether `SketchPlane.Create` from a grid's id gives
the vertical plane through it; whether `GetPlaneReference` names the level, reference plane or grid a work plane
was taken from, or only the work plane itself (then the reply falls back to its name); whether Revit lets a plan's
work plane be a vertical plane or a section's a level; whether a drafting view or a legend takes one at all (they
are refused here); and what `ShowActiveWorkPlane` does in a view that has none - LM2 to LM5 check them.

**Routing.** Measured through `heron_retrieve.find` on fresh scratch stores before and after (`model` backend, no
re-ranker; no filter, Revit 2020, 2024 and 2025, with and without the host's question flag), over every declared
sentence, every routing-table claim and eight probes; see
[row 5b-1392](../fragment-issues/section-5b-rows-401-425.md) for what moved.

**The model - `workplane-ip`**, a scratch project from the default architectural template, saved under
`%TEMP%\heron-work`, never a working model. Every write here is ROLLED BACK unless the modeller keeps it. The job
file is [`tools/jobs/set-view-work-plane-lm-2026-11-02.yaml`](../../tools/jobs/set-view-work-plane-lm-2026-11-02.yaml)
(`--dry-run` reads it back and sends nothing). Every line below with `HERON_CLIENT_ID=ajmal-pc`,
`--session <process id>` when two Revits are connected, and `< /dev/null`.

| # | Run | Look for |
|---|---|---|
| **LM1** | *Arrange.* `workplane-ip`: levels "Level 1" at 0 and "Level 2" at 4000 mm; a straight grid "B" 6000,0 to 6000,20000 mm; "Section 1" looking north across it, and in it one horizontal reference plane named "Ceiling Datum" 2700 mm above Level 1; a sheet "A101"; a drafting view "Drafting 1". Check that Work Plane > Set in "Level 1" says Current: Level : Level 1. Save | One grid, one named reference plane, a sheet |
| **LM2** | *The D-30 positive, and its negative - THE NEGATIVE FIRST.* The job file, `--write`, rolled back | Negative, the sheet A101: `changed` 0, refused naming A101 as a sheet, which has no work plane. Positive, plane "Level 2" in "Level 1": `changed` 1, `before` *Level : Level 1 - horizontal at elevation 0 mm*, `after` *Level : Level 2 - horizontal at elevation 4000 mm* |
| **LM3** | *Never guessed; the other kinds.* Plane "reference plane Ceiling Datum" in "Level 1"; plane "Level 9"; plane and display both left out; plane "Level 2" in "Drafting 1"; plane "B" in "Section 1" | Reference Plane : Ceiling Datum at 2700 mm; the second and third refused listing Level 1, Level 2, Ceiling Datum and B; the fourth refused naming a drafting view; the fifth what Revit says to a vertical plane in a section (made, or refused with Revit's words and nothing kept) |
| **LM4** | *Show, hide, already; the second route.* `display` show in "Level 1", then hide; plane "Level 1" in "Level 1"; after a kept positive, Work Plane > Set by hand; one Ctrl+Z | The work plane grid appears, then goes, each reply saying the display is not read back; the third `changed` 0, *already the work plane*; the dialog's Current line is the plane the reply named; one undo takes the change back |
| **LM5** | *The read, `REPORT_VIEW_WORK_PLANE` - THE NEGATIVE FIRST.* [`tools/jobs/report-view-work-plane-lm-2026-11-02.yaml`](../../tools/jobs/report-view-work-plane-lm-2026-11-02.yaml) (negative the sheet A101, positive "Level 1" as LM1 left it); then through `revit_read`: *"which work plane is this view on"* with "Level 1" in front and no view named; views "A101, Level 1"; Level 1's plane set by hand to Ceiling Datum and asked again | Negative: `workPlanes` empty, `withPlane` 0, the sheet in `findings` as "no work plane". Positive: `withPlane` 1, *Level : Level 1 - horizontal at elevation 0 mm*. Then one row for the view in front and a finding saying so; the plan its plane beside the sheet named as having none; the plan's row follows the hand change and nothing else in the answer changes |
