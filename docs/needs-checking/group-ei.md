# Needs checking — Group EI

> One group of [the register](../NEEDS-CHECKING.md), in its own file since 2026-10-16 so that it can be read
> alone. **The register's rules, and every group's place in it, are on that page.** A new row
> for this group goes in this file. [`tools/needs-checking-register.py`](../../tools/needs-checking-register.py)
> reads it back into the register for every tool that reads the register, so a row here is seen
> exactly as it was seen there.

## Group EI - the families open in the Family Editor, and a value a card may leave out: `REPORT_OPEN_DOCUMENTS` version 2 and `REPORT_FAMILY_FORMS` version 2 (2026-10-16)

Found on 2026-10-14 building two fixed monitor families on Revit 2020. `REPORT_OPEN_DOCUMENTS` answered
*"count 0 ... 2 family document(s) are open and are NOT listed above"* while Family1 and Family2 were open
in the Family Editor, so the chat could not tell the owner whether the 60 inch family had been saved - and
a family closed with no file is lost (a house and two towers were lost unsaved on 2026-10-13). And every
`REPORT_FAMILY_FORMS` call of both builds typed `which=all`, because the add-in refuses a text value left
out. [Row 5b-435](../fragment-issues/section-5b-rows-401-425.md) had the first gap since 2026-10-12;
[rows 5b-487 and 5b-488](../fragment-issues/section-5b-rows-401-425.md) have the rest.

**What changed.**
[`REPORT_OPEN_DOCUMENTS`](../../brain/fragments/report-open-documents/fragment.yaml) (FRG-DOC-026, DRAFT)
lists every family open in the Family Editor in `familyList` - one line, so a reply does not cut it to
three entries of sixty characters - each with its title, `[ACTIVE]` when it is the document read,
`[unsaved changes]`, its category, and its file or `NEVER SAVED, no file of its own`; `familyCount` counts
them, and a finding names every family and project that has never been saved, with Ctrl+S as the way to
keep it. The projects' `documents`, `titles`, `paths` and `count` are version 1's.
[`REPORT_FAMILY_FORMS`](../../brain/fragments/report-family-forms/fragment.yaml) (FRG-GEO-046, PROVEN)
takes `which` as `optional: true` with `absent: all`, and the add-in learned to read that: a need marked
optional that names its own default (`HeronBindingNote.DeclaredAbsent`) is bound, when the caller leaves it
out, exactly as if the caller had typed that text. Without `absent:` nothing changes - an optional text,
number or view left out is still refused (row 5b-227's three cards included). `family-creation` step 0
now asks the modeller once for Ctrl+S before a large build in a family with no file.

**What is compiled and what is not run.** Every fragment compiles on all eight releases 2020 to 2027, and
the add-in builds for all eight (`tools/check-compile.py`, 2026-10-16). `tests/test_binding_note.py` runs
the new rule in its test host. **None of it has run in Revit.** `REPORT_FAMILY_FORMS`' `impl/` did not move
- fingerprint `618a2699859aa0af` before and after, *proof stands* - so its 2026-10-03 proof (run with
`which` "all") still describes the code that runs; what is unproved is the ADD-IN filling the default in,
which works only once the add-in is rebuilt, deployed for each Revit and Revit restarted.

**A NEW SCRATCH PROJECT AND TWO SCRATCH FAMILIES, never a working model.** Make them before EI1, in
Revit 2020 first: a project from the default metric template saved as *Heron Open Docs Project.rvt*; a
family from *Metric Generic Model.rft* with one solid extrusion and one void extrusion, saved as *Heron Open
Docs Saved.rfa*; and a family from *Metric Furniture.rft* left **unsaved** (Revit names it Family1, or the
next free FamilyN), in front.

| # | Run | Look for |
|---|---|---|
| **EI1** | `revit_read` capability `REPORT_OPEN_DOCUMENTS`, the unsaved Furniture family in front | `count` 1 and the project in `documents` with its full path, exactly as version 1; `familyCount` 2; `familyList` on ONE line, whole: the FamilyN `[ACTIVE]` - Furniture - `NEVER SAVED, no file of its own`, and *Heron Open Docs Saved* - Generic Models - its full .rfa path; a finding naming FamilyN as never saved with Ctrl+S. The D-30 positive |
| **EI2** | Close both families (FamilyN without saving - it is scratch), then the same read with the project in front | `familyCount` 0, `familyList` empty, no NEVER SAVED finding, and the project listed exactly as in EI1 - **the D-30 negative**: nothing invented from the project |
| **EI3** | **After the add-in is rebuilt and deployed for Revit 2020 and Revit restarted, and the Claude app restarted** - the add-in binds the default, and the server's Python decides whether a request left without `which` is sent at all. *Heron Open Docs Saved* in front: `revit_read` capability `REPORT_FAMILY_FORMS` with **no values at all**; then the same with `which=all` | Both answers the same - `formCount` 2, `scanned` 2, the solid and the void - and the first one's binding note reads `which not given - "all", the card's own default (optional)`; never refused for a missing value |
| **EI4** | EI3's negative, same session: *Heron Open Docs Project* in front, `revit_read` capability `REPORT_FAMILY_FORMS` with no values | `notAFamily` true, `formCount` 0, nothing read - the default makes it run, never makes it read a project. The rule's other side - a card that names no default gains none, so row 5b-227's three cards are refused as before - is proved without Revit by `tests/test_binding_note.py`; no READ card carries such a value to try it on here |
| **EI5** | EI1 and EI3 once on Revit 2024 and once on 2027 | The same answers - the binder's change is in the add-in every release loads |
| **EI6** | Second route: View > Switch Windows, and Ctrl+S in the unsaved family | The same windows as EI1, one for one; Ctrl+S in FamilyN opens Save As rather than saving in place - Revit saying NEVER SAVED itself |
| **EI7** | *Not the proof - an observation.* In a project with a loaded family, Edit Family on it, then `REPORT_OPEN_DOCUMENTS` | What `familyList` says of a family opened from a project: its own .rfa path if Revit keeps one, or NEVER SAVED. Write down which; if NEVER SAVED, the finding's "closing it loses the family" is too strong for a family the project still holds, and that sentence needs a version 3 |
| **EI8** | *Watching, not a proof.* A chat starts a large fixed-family build in a family that has never been saved | Before the first write it asks once for Ctrl+S, as `family-creation` step 0 now says, and does not ask again |
