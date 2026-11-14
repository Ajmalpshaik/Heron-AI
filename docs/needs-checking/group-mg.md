# Needs checking — Group MG

> One group of [the register](../NEEDS-CHECKING.md), in its own file so that it can be read alone. **The
> register's rules, and every group's place in it, are on that page.** A new row for this group goes in
> this file. [`tools/needs-checking-register.py`](../../tools/needs-checking-register.py) reads it back
> into the register for every tool that reads it, so a row here is seen exactly as it was seen there.

## Group MG - Analyze > Analytical Model - draw analytical members, panels and links, move or redraw an analytical line or outline without moving the element, and tie an analytical element to its physical one: `CREATE_ANALYTICAL_ELEMENTS`, `ADJUST_ANALYTICAL_ELEMENTS`, `ASSOCIATE_ANALYTICAL_ELEMENTS` (2026-11-02)

Asked for in the modeller's words: *"add an analytical member between these two points"*, *"create an
analytical panel from these corners on Level 2"*, *"link these two analytical nodes with an analytical
link"*, *"move the analytical line of this beam 200 mm down"*, *"associate this analytical member with that
beam"*. Nothing in Heron wrote an analytical element: `brain/heron_retrieve.py` on a scratch store sent the
five to `REPORT_ANALYTICAL_MODEL` (a read), `DRAW_FAMILY_POINT_CURVES` and the rest of a shortlist of
unrelated writes, and no fragment's C# called `AnalyticalMember.Create`, `AnalyticalPanel.Create`,
`AnalyticalLink.Create`, `SetCurve`, `SetOuterContour` or `AddAssociation`. `MOVE_ELEMENTS` (PROVEN) moves the
beam itself and was left untouched; `REPORT_ANALYTICAL_MODEL` (DRAFT) reads the model and gained one
declared sentence, outside `impl/` (below).

| What | Where | What it does |
|---|---|---|
| `CREATE_ANALYTICAL_ELEMENTS` (MODIFY) | [`brain/fragments/create-analytical-elements`](../../brain/fragments/create-analytical-elements/fragment.yaml) | ONE a run: a member between exactly two points, a panel through three or more corners, or a link between exactly two analytical nodes selected. Points in mm; a level named measures every height up from it; a link type named. A level or type not there, or a link with no type, is refused with the list. Reads back the id and a member's ends, a panel's corners or a link's ends and nodes |
| `ADJUST_ANALYTICAL_ELEMENTS` (MODIFY) | [`brain/fragments/adjust-analytical-elements`](../../brain/fragments/adjust-analytical-elements/fragment.yaml) | Moves the analytical line of beams, columns and braces, or the analytical outline of floors and walls, by an `offset` in mm - or redraws ONE through new `points` - never the physical element. A physical element reaches its analytical one through Revit's association; one with none is named in `noAnalytical`. Each read back before and after; one not within 1 mm of where it was asked to go is `skipped`, not counted |
| `ASSOCIATE_ANALYTICAL_ELEMENTS` (MODIFY) | [`brain/fragments/associate-analytical-elements`](../../brain/fragments/associate-analytical-elements/fragment.yaml) | Ties one analytical member or panel to one physical element, both selected. An association either already has is refused, naming it, unless `replace` is true - then the old tie is removed first and named. A removal followed by a refused tie throws, so the whole change rolls back. Read back |

None deletes or moves an element, physical or analytical, beyond the analytical geometry asked for.

**What is done and what is not.** All three compile for every release they claim, **2023 to 2027**
(`tools/check-fragments-compile.py`, 2026-11-02, a Linux container). Every Revit member was read off the
shipped assemblies of all eight releases with `tools/api-surface --members`: `AnalyticalMember`,
`AnalyticalPanel` and `AnalyticalToPhysicalAssociationManager` are absent 2020-2022, so the cards claim
2023-2027 and Heron refuses them by name on the older three. `AnalyticalLink` and `Hub` exist 2020-2027, but
the link half is not claimed on 2020-2022 until a run there shows what an analytical node is. **Nothing here
has run in Revit.** All three are DRAFT. **Only Revit can settle**, and MG2-MG6 record: whether a member or
panel drawn this way is accepted and what its type and role read; whether the analytical nodes selected in
a 3D view pass `AnalyticalLink.IsValidHub`; whether `SetCurve` on a beam's analytical member holds or is
pulled back by the association; and Revit's words when it refuses a tie.

**Routing.** Measured through `heron_retrieve.find` on fresh scratch stores of the tree at `ef290af` and of
this change, Revit 2024, with and without the host's question flag (the `READ` ceiling), over every declared
utterance and routing-table sentence of a live card and ten probes. Every sentence the three cards declare
reaches its card by identity. *"export the analytical model"* - `REPORT_ANALYTICAL_MODEL`'s own routing
row - went to `CREATE_ANALYTICAL_ELEMENTS` without the flag once the card landed; it is now declared on
`REPORT_ANALYTICAL_MODEL` and reaches it again
([row 5b-1434](../fragment-issues/section-5b-rows-401-425.md), FIXED). Three made-up questions - *"is this beam associated with an analytical member"*, *"what analytical
links are in this model"*, *"where is the analytical line of this beam"* - reach the new writes without the
flag and `REPORT_ANALYTICAL_MODEL` with it ([row 5b-1435](../fragment-issues/section-5b-rows-401-425.md),
OPEN). No other card's declared sentence lost the card it reached, and no routing-table sentence lost the
card that claims it; *"raise the ducts 200"* now reaches `MOVE_ELEMENTS`, which claims it.

**The model - a scratch project from the metric structural template, Revit 2023 to 2027.** Never a working
model. Every run below is rolled back unless the modeller keeps it. The job file is
[`tools/jobs/analytical-elements-mg-2026-11-02.yaml`](../../tools/jobs/analytical-elements-mg-2026-11-02.yaml)
(the create and the adjust; `--dry-run` reads it back and sends nothing). Every line below with
`HERON_CLIENT_ID=ajmal-pc`, `--session <process id>` when two Revits are connected, and `< /dev/null`.

| # | Run | Look for |
|---|---|---|
| **MG1** | *Arrange.* New project from the metric structural template, Revit 2024, saved as `Heron MG Analytical` under `%TEMP%\heron-work`: Level 1 and Level 2, columns C1 and C2, beam B1 between them on Level 2, floor S1 on Level 2, an architectural wall A1 (its Structural box off), the analytical model on. Read it with `REPORT_ANALYTICAL_MODEL` | Members for C1, C2 and B1, a panel for S1, nothing for A1 |
| **MG2** | *Create - the D-30 positive, and its negative - THE NEGATIVE FIRST.* The job file's first job, `--write`, rolled back | Negative, level "Level 9": `created` 0, refused listing every level with its elevation. Positive, a member from 0,0,0 to 6000,0,0 on Level 2: `created` 1, its ends at Level 2's elevation, 6000 mm long |
| **MG3** | *Adjust - the D-30 positive, and its negative - THE NEGATIVE FIRST.* The job file's second job, `--write`, rolled back | Negative, the wall A1: `adjusted` 0, `noAnalytical` A1. Positive, B1, offset 0,0,-200: `adjusted` 1, both ends of B1's analytical member 200 mm lower, read back; B1's own Location line unchanged in its Properties |
| **MG4** | *Panel and link, never guessed.* `validate create-analytical-elements --write` with `kind=panel`, `points=0,0,0; 6000,0,0; 6000,4000,0; 0,4000,0`, `level=Level 2`; then C1's and C2's analytical nodes at Level 2 selected in a 3D view, `kind=link` with `type` left out, and again with a type from that list | A panel with 4 corners at Level 2; the link without a type refused with every analytical link type - never a type called "none" (row 5b-1432); with the type, `created` 1 and the two nodes named. RECORD whether the nodes selected pass as nodes |
| **MG5** | *Associate - by name, THE NEGATIVE FIRST.* An analytical member M9 drawn by MG4's first line and KEPT with `try-once`; then `validate associate-analytical-elements --write` with M9 and B1 selected (`replace` left out); the same with `replace=true`; then M9 and a beam whose analytical member was deleted in Revit | Negative: `associated` 0, refused naming B1's own member - nothing changed. `replace=true`: `associated` 1, the old tie named as removed first. The beam with none: `associated` 1. Read each with `REPORT_ANALYTICAL_MODEL` |
| **MG6** | *The second route, and the other releases.* MG3's positive kept once with `try-once`, B1's analytical member seen in a 3D view with the analytical model shown; MG2 on a project made the same way in Revit 2023 and in Revit 2027, and on Revit 2022 | The member 200 mm below B1, B1 where it was. 2023 and 2027 the same answers; 2022 refused by name - the cards claim 2023-2027 |

### Run record - 2026-11-14 night, Revit 2024 (session 3592), the owner asleep

**MG1 - arranged by Heron, and MG1's own reading fails on Revit 2024.** `CREATE_FAMILY_DOCUMENT` from Revit 2024's
`English\Structural Analysis-DefaultMetric.rte` to `%TEMP%\heron-work\mg\Heron MG Analytical.rvt` (Level 1 at 0,
Level 2 at 3000; its plans are *Structural Plans*, `EngineeringPlan`). Kept with PROVEN cards: C1 421963 and C2 421964
(`PLACE_FAMILY_INSTANCES structuralType=column`, *M_Concrete-Rectangular-Column: 300 x 450mm*, top *"read back as
asked on all 2"*), B1 421966 (`PLACE_LINE_BASED_FAMILY`, *M_Concrete-Rectangular Beam: 300 x 600mm*), S1 421972
(`CREATE_FLOOR`, *Insitu Concrete 225mm*), and *Generic - 200mm* walls with Structural off on Level 1 and Level 2 (A1).
`REPORT_ANALYTICAL_MODEL`: *"0 analytical member(s), 0 analytical panel(s)"* - and `READ_ELEMENT_PARAMETERS
parameterName=Enable Analytical Model` finds the parameter absent on all four. From Revit 2023 a physical element gets
no analytical element by itself, so *"Members for C1, C2 and B1, a panel for S1"* cannot be read on this release
until they are made and tied (MG4, MG5). The job file for this model is
`tools/jobs/analytical-elements-mg-revit2024-2026-11-14.yaml` - `1 PASS, 1 POSITIVE EMPTY`.

| # | Result | What was seen |
|---|---|---|
| MG2 | **PASS** | Negative *Level 9*: *"There is no level called "Level 9". The levels are: ..."*, `created 0`, *"No analytical element was drawn - the model is as it was."* Positive: *"Drew analytical member 421980 from (0, 0, 3000) mm to (6000, 0, 3000) mm, 6000 mm long, heights measured from "Level 2" at 3000 mm. It is tied to no physical element"*, `created 1`. Draft written, gap *no second route* |
| MG3 | **POSITIVE EMPTY - the arrangement, as expected on 2024** | B1 selected in `EngineeringPlan: Level 2`: *"1 element(s) have no analytical element, so nothing was moved for them: Structural Framing "300 x 600mm" (421966)"*, `adjusted 0`. The negative never ran - `needs_unbound`: the Level 2 Structural Plan shows no non-structural wall, so A1 was never selected (rule 3). Owed once B1 has a member tied to it, which needs MG4's member KEPT and MG5 - a DRAFT kept is the owner's `try-once` |
| MG4 (panel) | **PASS** - by `validate --write`, rolled back, kept apart in `%TEMP%\heron-work\mg\mg4-panel.json` | *"Drew analytical panel 421990 with 4 corners (0, 0, 3000) mm; (6000, 0, 3000) mm; (6000, 4000, 3000) mm; (0, 4000, 3000) mm"*, `created 1`. Two corners: *"An analytical panel needs three or more corners; 2 were given ... Nothing was drawn."*, `created 0` |
| MG4 (link), MG5, MG6 | **NEEDS THE OWNER** | Each needs analytical members or nodes KEPT first - the owner's Keep on `try-once` - or Revit 2022/2023/2027 |

*Heron MG Analytical* holds, kept: C1, C2, B1, S1 and the two walls; no analytical element.

**Signed 2026-11-15, as Ajmal PS, on the owner's word** (*"Yes, sign all ten"*), commit 6c3c49d3 - `create-analytical-elements` PROVEN (MG2).
