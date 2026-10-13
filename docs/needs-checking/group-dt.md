# Needs checking — Group DT

> One group of [the register](../NEEDS-CHECKING.md), in its own file since 2026-10-13 so that it can be read
> alone. **The register's rules, and every group's place in it, are on that page.** A new row
> for this group goes in this file. [`tools/needs-checking-register.py`](../../tools/needs-checking-register.py)
> reads it back into the register for every tool that reads the register, so a row here is seen
> exactly as it was seen there.

## Group DT - colour schemes: APPLY_COLOR_FILL_SCHEME version 2 and REPORT_COLOR_FILL_SCHEME, the read and the check (2026-10-13)

Found while styling the futuristic-house plans in Project2 (Revit 2020.2): Heron could not colour a plan
by room name, and `heron_lookup` offered three other tools. [Row 5b-447](../FRAGMENT-ISSUES.md) has the
measurements.

**What changed.** [`APPLY_COLOR_FILL_SCHEME`](../../brain/fragments/apply-color-fill-scheme/fragment.yaml)
version 2 (FRG-VIEW-110, widened, DRAFT): several views or a view template; a colour band (vivid, pastel,
neon) and/or a colour per value; a scheme named in `schemeToUse`, used if there and made by copying if not;
the legend beside the crop region, at a point in millimetres, or none; and `clearScheme`. Every new input
is EMPTY (the bool absent) for version 1's behaviour, and the one job file that runs the card passes them
empty. **New:** [`REPORT_COLOR_FILL_SCHEME`](../../brain/fragments/report-color-fill-scheme/fragment.yaml)
(FRG-VIEW-136, READ, DRAFT) - each view's scheme per category, every row and colour, the legends, and the
checks: a coloured plan with no legend, two rows in one colour, values shown with no row, rows nothing
uses, a template that holds the scheme. A new card rather than a widening because a read must run with no
transaction and no Changes switch; the card's comments say so.

**Both declare 2020 to 2027 and compile on all eight** (`tools/check-fragments-compile.py`). The writes are
2022 and later: 2020's API has no colour scheme type at all, 2021's has no legend, so those releases refuse
in plain words and change nothing.

**What was run, and it is NOT a proof of the colouring.** Revit 2020.2, a new scratch project "Project1"
(session 18392, 2026-10-13), never saved: walls and two rooms (Living, Kitchen) built with
`CREATE_WALL`, `PLACE_ROOM_AT_POINT` and `WRITE_ELEMENT_PARAMETERS`; the plan "1 - Mech" copied with
`DUPLICATE_VIEW` and renamed "Level 1 - Colour Test". On the copy, Rooms by Name: refused in plain words,
naming the model's 9 colour schemes and the steps in Revit's own dialog. Negatives, each refused before
any write: a parameter rooms do not carry, the Level 2 plan with no rooms, `colours` Kitchen=300,0,0,
`legend` beside, and `clearScheme`. **3,184 elements before and after.** `REPORT_COLOR_FILL_SCHEME` on the
copy and the original read 0 legends and said which scheme a view shows is not readable in 2020. A
scratch probe found the plan's "Color Scheme" property is a parameter with no value (`StorageType.None`)
and the scheme elements expose no category or parameter - there is no 2020 route to wire.

Never on a working project. With `HERON_CLIENT_ID=ajmal-pc`, `--session <process id>` when two Revits are
connected, and `< /dev/null` on every line.

| # | Run | Look for |
|---|---|---|
| **DT1** | **Revit 2022 or later** (2024, 2026 and 2027 are on the PC): a scratch project with rooms; copy a plan with `DUPLICATE_VIEW`; `APPLY_COLOR_FILL_SCHEME` on the COPY, Rooms, Name, the three strings empty | `schemeApplied` true, `viewsChanged` 1, one row per room name with its RGB, `legendsInView` 1. Then `REPORT_COLOR_FILL_SCHEME` on the copy AND the original: the copy lists the same rows; the original reads as it did before |
| **DT2** | The same copy, `colours` "pastel; Kitchen=255,200,200" | every row on the pastel steps and Kitchen 255,200,200, each read back; when another view shows the scheme, the colours go on a COPY and `findings` says so |
| **DT3** | `schemeToUse` "TRG Rooms Presentation" with Name, run twice | made by copying the first time, USED the second - no "(2)" |
| **DT4** | `legend` "12000,4000" where a legend is already there, then `legend` none on a second plan | the legend moved to that point and read back; no legend placed on the second |
| **DT5** | `clearScheme` true on the copy | the Rooms scheme reads back as none, the copy's Rooms legend gone, the scheme element still in the model, every other view unchanged |
| **DT6** | A plan whose view template holds the colour scheme; then the request naming THE TEMPLATE as the view | the plan refused naming the template and how many views follow it; the template takes the scheme, with a note that a template carries no legend |
| **DT7** | The negatives on 2022+: a parameter rooms do not carry; the {3D} view; `clearScheme` on a plan with nothing to clear | each refused before any write, the element count unchanged |
| **DT8** | `REPORT_COLOR_FILL_SCHEME`'s checks: delete the legend by hand, give two rows one colour | the missing legend and the shared colour each named in `findings` |
| **DT9** | **Revit 2021** - no Revit 2021 is on this PC | the plain refusal, as on 2020. Compiled only |
| **DT10** | **After the merge**, with the main checkout fast-forwarded - the shared store then rewrites the changed card's row and adds the new one by itself (`heron_scope.refresh`, row 5b-406): `heron_lookup` "colour the rooms by name" with Revit 2020 connected | `APPLY_COLOR_FILL_SCHEME`, no longer filtered out; "which colour scheme does this view use" -> `REPORT_COLOR_FILL_SCHEME` |
| **DT11** | The signatures, on the owner's word only, after DT1 to DT8 | `python brain/heron_validate.py accept <fragment> --by "Ajmal PS"` for each |
