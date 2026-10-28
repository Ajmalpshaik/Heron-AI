# Needs checking — Group JI

> One group of [the register](../NEEDS-CHECKING.md), in its own file since 2026-10-24 so that it can be read
> alone. **The register's rules, and every group's place in it, are on that page.** A new row
> for this group goes in this file. [`tools/needs-checking-register.py`](../../tools/needs-checking-register.py)
> reads it back into the register for every tool that reads it, so a row here is seen
> exactly as it was seen there.

## Group JI - curtain panels changed by where they sit on one wall - a row, a column, a floor line: `SET_CURTAIN_PANEL_TYPE` version 1 (2026-10-28)

A new card, [`set-curtain-panel-type`](../../brain/fragments/set-curtain-panel-type/fragment.yaml),
written in a cloud session for the modeller's *"change the bottom row of panels on this curtain wall to
solid panels"*, *"put spandrel panels on every floor line"* and *"change panel type in the second
column"*. Nothing in the library changed a curtain panel by its place on a wall: the
[curtain-wall skill](../../brain/skills/curtain-wall.yaml) listed the panel type as NOT YET, and
`SET_CURTAIN_WALL_GRID`'s routing said nothing did. One selected panel was already served -
`CHANGE_ELEMENT_TYPE` (PROVEN) made three panels into doors on 2026-10-13
([building-shell](../../brain/skills/building-shell.yaml), step 9) - so this card takes the curtain
wall ITSELF selected and refuses a selection of panels, naming that card. The refusal is also a guard:
*"make these curtain panels glazed"* landing here cannot change every panel of the wall.

**The modeller's two selected-panel sentences are not declared anywhere, and JI12 is why.** Declared on
`CHANGE_ELEMENT_TYPE` (card text only), they moved its vector from nearness #4 to #5 for
[`tests/test_routing_phrases.py`](../../tests/test_routing_phrases.py)'s guarded question *"read the
thermal U value of every floor type and roof type"* - an exact tie, 0.0316 against 0.0316, on the base
tree - and the question reached `SET_MATERIAL_PROPERTIES`, a write. Declaring the question on
`REPORT_SPACE_ENVELOPE` instead is the repair the test names, but it asks for the sentence to move from
the test's QUESTIONS table to its DECLARED one: a test edit, which is the owner's. Both were undone.
Undeclared, the two sentences reach this card on the cloud's backend, and are refused here.

**What Revit's API says.** Read with `tools/api-surface` on all eight releases, 2020 to 2027, on
2026-10-28: `CurtainGrid.ChangePanelType(Element, ElementType)` returning `Element`, `GetPanelIds`,
`GetUnlockedPanelIds`, `Panel.FindHostPanel` and every other member the card calls carry one signature
on every release. `RevitAPI.xml` (the Nice3point 2024 package) says that on a curtain wall the new type
may be a `PanelType` or a `WallType` - so a basic wall can be the infill - and on a curtain system only
a `PanelType`; it names `ArgumentException` (the type cannot be used for the panel) and
`InvalidOperationException` (the change failed).

**What is done and what is not.** The card compiles for every release 2020 to 2027
(`tools/check-fragments-compile.py`, 2026-10-28, in the cloud), and its three sentences reach it on this
container's backend. **Nothing has met a Revit.** `revit_change` refuses to keep a change from it until
JI14 is signed (D-117). Five things no reference settles, and each row below that runs one records it:

- that ChangePanelType changes a panel the wall's TYPE placed, which Revit shows pinned, without
  unpinning it - `CHANGE_ELEMENT_TYPE`'s `ChangeTypeId` did on 2026-10-13. `measured` counts the chosen
  panels Revit calls pinned and locked;
- that a curtain wall door type is a `PanelType` to the API - `measured` prints the class Revit gives
  the named type;
- that a door panel changes back to a glazed or solid one the same way;
- which changed panels come back with a new id (a panel becoming a wall cannot keep its own), and
  whether the wall's list of cells then names the wall or the panel it stands in for;
- that a panel's change needs its wall editable too - which is why a wall another user holds refuses
  the whole request.

**How rows and columns are counted** - read off the wall, never off Revit's U and V: row 1 is the
BOTTOM; column 1 is at the wall's START end, the end it was drawn from, and `layout` says whether that
is its left or right end seen from outside. A panel merged across a grid line is in both rows or
columns. A floor line is the row a level crosses; a level within 5 mm of a grid line crosses none and is
named.

**The rig (JI0).** The owner's Project1 house (Revit 2020, DefaultMetric), **saved** as *Heron JI
Panels.rvt* in `%TEMP%\heron-work\group-ji` - a run needs a saved model. The rolled-back rows build
their own wall inside the run: a 9000 mm by 3000 mm wall of *Curtain Wall: Curtain Wall 1* from
`0,60000,0` to `9000,60000,0` on *Level 1* (look in plan that this is clear of the house), its type
copied as *HERON CW PANEL PROOF*, the wall put on the copy, and the copy given a fixed 1500 mm grid both
ways - six columns of 1500 and two rows of 1500, twelve panels, derived in the card's
[cases](../../brain/fragments/set-curtain-panel-type/tests/cases.yaml).
[`tools/jobs/set-curtain-panel-type.yaml`](../../tools/jobs/set-curtain-panel-type.yaml) runs JI2 and
JI3. For JI1 the same wall is made BY HAND and saved: drawn, its type duplicated as *HERON CW PANEL
PROOF*, and Vertical and Horizontal Grid set to Fixed Distance 1500 in Type Properties.

| # | Run | Look for |
|---|---|---|
| **JI0** | The rig, by hand | Project1 saved as *Heron JI Panels.rvt*; nothing selected; Level 2's elevation read off an elevation view, for JI6 |
| **JI1** | *The kept run.* JI0's hand-made wall selected - the wall itself, Tab to it - an elevation of it in front; `panelTypeName=System Panel: Solid`, `whichPanels=bottom row` - once through `try-once`, on the owner's yes | Revit redraws the elevation with the bottom six panels solid and asks Keep or Discard. Keep: `changed` 6, `layout` 2 rows and 6 columns of 1500; one Ctrl+Z takes it back as one entry. Discard leaves nothing. A kept run is the positive half of the proof (D-120) |
| **JI2** | *The batch positive, rolled back.* The job file | `chosen` 6, `changed` 6, `differs` and `skipped` empty; `measured` Before 12 x *System Panel: Glazed* - the copy's own panel, read and not assumed - and After 6 x Glazed, 6 x Solid; the class Revit gives *System Panel: Solid*; the pinned and new-id counts written down here |
| **JI3** | *Negative.* The job file's second leg: the same request on a BASIC wall | `changed` 0, refused *"None of what is selected is a curtain wall"*, `layout` and `measured` empty |
| **JI4** | *A wall as the infill.* JI2 with `panelTypeName=Basic Wall: Generic - 200mm`, `whichPanels=top row` | `changed` 6; After 6 x *Basic Wall: Generic - 200mm*; how many came back with a new id - **not known** which |
| **JI5** | *Columns and one cell.* `whichPanels=column 2`, then `row 1 column 3` | `chosen` 2, then 1. In an elevation from outside, the changed column is the second from the end `layout` names as column 1's |
| **JI6** | *Floor lines.* JI2 with `height=6000` (four rows of 1500), `whichPanels=floor lines` | `layout` names each level crossing the wall and its row - Level 2 at 4000 mm in row 3 if JI0 read it so; a level at 3000 or 4500 reads *"is ON a grid line"* and adds nothing |
| **JI7** | *A curtain wall door, and back.* The owner loads a curtain wall door family first (`LOAD_FAMILY`, his call); `whichPanels=row 1 column 3` with that door type, then the same cell with *System Panel: Glazed* | `changed` 1 both times; `measured` names the class Revit gives the door type - **not known** whether `PanelType` |
| **JI8** | *Refusals.* `panelTypeName=HERON NO SUCH PANEL`; `whichPanels=row 3`; `row 1 column 7`; `diagonal`; `panelTypeName=Curtain Wall: Curtain Wall 1`; two curtain walls selected; ONE PANEL of JI1's wall selected instead of the wall | Each refused naming why - the panel *"1 curtain panel(s) are selected, not a curtain wall"*, naming `CHANGE_ELEMENT_TYPE` - - the first WITH the model's panel, door, window and wall types (and it corrects JI2's guess of *System Panel: Solid* if wrong); `changed` 0 every time |
| **JI9** | *Already so.* `panelTypeName=System Panel: Glazed`, `whichPanels=all` | `chosen` 12, `changed` 0, `alreadyThatType` 12 - not a refusal |
| **JI10** | *Pinned and new ids, read.* From JI1, JI2 and JI4's `measured` | Whether the type's own panels counted pinned, and whether they changed all the same; how many ids were new for a panel type and for a wall. Written here, then into the card's comments |
| **JI11** | *A shared model.* A central copy where another user holds one panel of the wall | That panel in `skipped` *"held by ..."*, the rest changed. **Needs two users** - NOT RUN until a central model is to hand |
| **JI12** | *Routing, on the PC's backend.* The three sentences; *"make these curtain panels glazed"* and *"swap the curtain panel to a door panel"*; *"which panel types are on this wall"*; and the paraphrases below | The three reach `SET_CURTAIN_PANEL_TYPE`; the question reaches a reader, `REPORT_CURTAIN_ELEMENTS`, never this write (D-86); `tests/test_routing_phrases.py` passes but for its two failures that were there before this card. **Measured through `find` on a private store on this container's trained model, 2026-10-28, with the card as committed:** the three, by identity, here; *"which panel types are on this wall"*, *"what curtain panel is this"* and *"draw a curtain wall"* at `REPORT_CURTAIN_ELEMENTS`; *"select all elements of type Curtain Wall"* at `REPORT_CURTAIN_WALL_TYPE`; *"read the thermal U value of every floor type and roof type"* at `REPORT_SPACE_ENVELOPE`. **OPEN:** the two selected-panel sentences, and *"change this panel to solid"*, reach this card, which refuses a selection of panels and names `CHANGE_ELEMENT_TYPE` - the owner decides whether the guarded thermal question moves to the test's DECLARED table so they can be declared there. And three questions I wrote to probe it - *"what panels are in the bottom row of this curtain wall"*, *"which panels on this curtain wall are solid"*, *"what type is the panel in the second column"* - reached this write. None is a sentence a modeller was recorded saying, so none was declared; no reader names a panel's type by its row or column yet |
| **JI13** | *Another release.* JI2 on Revit 2024 | The same answer |
| **JI14** | The signature, on the owner's word only, after JI1 to JI10 | `python brain/heron_validate.py accept set-curtain-panel-type --by "Ajmal PS"` |

A row that fails stays open; nothing in a row changes to make it pass.
