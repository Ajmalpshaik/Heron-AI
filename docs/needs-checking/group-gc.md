# Needs checking — Group GC

> One group of [the register](../NEEDS-CHECKING.md), in its own file since 2026-10-17 so that it can be read
> alone. **The register's rules, and every group's place in it, are on that page.** A new row
> for this group goes in this file. [`tools/needs-checking-register.py`](../../tools/needs-checking-register.py)
> reads it back into the register for every tool that reads the register, so a row here is seen
> exactly as it was seen there.

## Group GC - one family swapped for another, whatever each one's hosting: the method `replace-in-place-family` widened (2026-10-17)

**What was wrong.** The method [`replace-in-place-family`](../../brain/skills/replace-in-place-family.yaml)
swapped only IN-PLACE families. On 2026-10-15, on Revit 2020, 13 builders-work openings
(`TCM_GEN_CutOut_Rectangular_NEW` and `TCM_GEN_CutOut_Cirular_NEW`, Generic Models hosted on a face of the
linked structural model) were swapped by hand for `TRG_GEN_CutOut_Rectangular_HVAC : 12.5mm Cutout Buffer`,
a level-based family, in a real project. The first delete silently took 33 tags and dimensions, the copies
landed on the active workset, and a copy brought its source's Wall Width with it.
[Row 5b-740](../fragment-issues/section-5b-rows-401-425.md).

**What changed - the same method, widened; no second method.** It now swaps a loadable family too, any
category, any hosting on either side: it reads what goes with the old ones before anything changes, agrees
one table of values with the modeller (carried by GUID, a size map the modeller gives, nothing inherited silently),
tries a type change first where the two families host the same way, places one new one, shows it, then the
rest - by `PLACE_FAMILY_INSTANCES`, `PLACE_FAMILY_ON_FACE` or `PLACE_HOSTED_FAMILY` by the new one's hosting,
or by copying an existing instance of the new type and moving it where the placement refuses - puts each on
its old one's workset, copies the tags with `COPY_TAGS_TO_REPLACEMENTS`, and names every tag and dimension a
delete takes before asking for it. **The GUID write is still a gap**: `SET_PARAMETER_VALUES_BY_ID`
version 2 finds a parameter by name, measured from its code on main 2026-10-17.

**What is measured and what is not run.** The method validates (`python brain/heron_skill.py`); every
capability it names has a provider; which methods `heron_lookup` names beside every declared sentence and the routing phrase lists was
measured on main's skills and this branch's, on both search backends and with the re-ranker - the figures are
in row 5b-740.
**Nothing of it has run in Revit.**

**A NEW PROJECT, never a working model, and never any `4355-*` model.** Make it on **Revit 2020** first -
the release of the first use:

- a structural model with two walls and a floor, saved, then LINKED into a new project from the default
  template, **workshared**, with worksets `MEP` and `Builders Work` beside the default one;
- two face-based Generic Model families made for it, `TEST_CutOut_Rect_Old` and `TEST_CutOut_Round_Old`,
  each with a shared instance parameter `Opening Tag`, a shared `Width` and `Height`, and - the trap from
  2026-10-12 - a SECOND shared parameter also called `Opening Tag` with another GUID, left empty;
- a level-based Generic Model family `TEST_CutOut_New` with its insertion point at its plan centre,
  instance `Object Length`, `Object Height` and `Elevation from Level`, an instance `Wall Width`, BOTH
  shared `Opening Tag`s, and a TYPE parameter `Buffer` on two types;
- four old openings hosted on faces of the LINKED walls - three rectangular, one round, two on Level 1 and
  two on Level 2 - each on workset `Builders Work`, each tagged in a plan and a section, two of them
  dimensioned to a grid;
- one `TEST_CutOut_New` placed anywhere, on workset `MEP`, with `Wall Width` set to 300 - the copy source.

Every change is shown in Revit's Keep or Discard box (D-126); a DRAFT step runs once there (D-119).

| # | Run | Look for |
|---|---|---|
| **GC1** | `heron_method replace-in-place-family` in a new chat | The method whole: the name naming in-place OR loadable and any hosting, steps 0 to 10, the GAP at step 7 only, and every capability in its `needs` with its status - `COPY_TAGS_TO_REPLACEMENTS` DRAFT. `PLACE_FAMILY_ON_FACE` and `PLACE_HOSTED_FAMILY` are named in step 6 and not listed; row 5b-740 says why |
| **GC2** | **Routing, on the owner's PC** (trained model, with the re-ranker if installed): `heron_lookup` with *"replace these openings with the TRG family at the same place"*, then *"swap this family for that one keeping their data"*, then *"copy this opening to the other level"* | The first two name `replace-in-place-family` among the methods - row 5b-740 says which of them reached it in a container and which did not; the third does not name it. Write down what each answers - the capability and the methods |
| **GC3** | Steps 0 and 1: `SELECT_BY_FAMILY` `familyName=TEST_CutOut_Rect_Old` `exactMatch=true`, then the round one; `REPORT_PARAMETER_INVENTORY` on the old ones as step 1 says; `REPORT_ELEMENT_DEPENDENCIES` on them | 3 and 1 found; the record names each one's Host as a face of the link, its level, workset `Builders Work`, both `Opening Tag` GUIDs (`sameNameTwice` 1 per element) and its tags; the dependents name every tag and both dimensions |
| **GC4** | Step 2: `REPORT_PARAMETER_INVENTORY` on the copy source; then the table the chat puts to the modeller | The table lists `Wall Width = 300` as a value a copy would keep that nobody chose, `Buffer` as a type value, `Opening Tag` paired by GUID, and asks for the size map. **Nothing is placed before the yes** |
| **GC5** | Step 5, the type change. A second pair, both LEVEL based - copy `TEST_CutOut_New` as `TEST_CutOut_New2` with one changed shape, load it, place one `TEST_CutOut_New`, tag it, then ask for it to be swapped | `CHANGE_ELEMENT_TYPE` on that one: the same element id, its tag still on it, its values where both families carry them. **The negative:** step 5 tried on a face-based old one against the level-based new type - refused or not taken, named, and the chat goes on to step 6 |
| **GC6** | Step 6, placement, on the first old one: `PLACE_FAMILY_INSTANCES` at its box centre on its level | Where it stands: its box centre against the old one's within 1 mm. If it stands elsewhere (as on 2026-10-15, 2152 asked and 2014.5 got), the chat says so and takes the copy route - not a second guess at the height |
| **GC7** | Step 6, the copy route: `FILTER_ELEMENTS_BY_ID` on the copy source, `COPY_ELEMENTS` by the plan step for a Level 1 old one and with `toLevels=Level 2` for a Level 2 one, `Elevation from Level` by `SET_PARAMETER_VALUES_BY_ID`, then `MOVE_ELEMENTS` that copy alone | Each copy's box centre on its old one's within 1 mm; `MOVE_ELEMENTS` `moved 1`, `partly` and `blocked` empty; the Level 2 copy on Level 2 |
| **GC8** | Step 6, the turn and the workset: the openings on a wall NOT parallel to a grid (say 30 degrees); then `LIST_WORKSETS` and `SET_ELEMENT_WORKSET` | Each copy runs along its wall as the old one did - its hand, not its facing, compared; the copies were on the ACTIVE workset until moved, then all on `Builders Work`, read back |
| **GC9** | Step 6, one first: the chat shows the first new one and waits | No second one is placed before the modeller's word |
| **GC10** | Step 7, the values, by the agreed map: `Object Length` and `Object Height` from the size map, `Wall Width` blanked as agreed, and `Opening Tag` | The first three written and read back. **The GAP:** both families carry two `Opening Tag`s, so `SET_PARAMETER_VALUES_BY_ID` refuses that name - the chat names the gap, gives the value from the old one's FILLED GUID, and asks; it never picks one of the two by name |
| **GC11** | Step 8, the compare | 0 missing, 0 different, 0 not chosen. **The negative:** leave one copy's `Wall Width` at 300 on purpose - the compare names it as a value nobody chose |
| **GC12** | Step 9: `COPY_TAGS_TO_REPLACEMENTS` with the old-new pairs | Each tag made on its new one in the same view, type and head; `notReproduced` names any leader that differs; a second run makes none; the old tags still there |
| **GC13** | Step 10: `REPORT_ELEMENT_DEPENDENCIES` again, the yes question, then `DELETE_ELEMENTS` | The question names the 4 openings, the tags copied, and the 2 dimensions that will go; `deleted` holds nothing the question did not name. **The negative:** answer no - nothing deleted |
| **GC14** | `PLACE_FAMILY_ON_FACE` with `searchLinks=true` on a face-based new type, at one old one's point, ray towards the linked wall | Write down what Revit does: hosted on the link's face, or refused. Its card says both - version 2 *"a face in a loaded link hosts too"*, an older paragraph *"A REFERENCE INTO A LINKED MODEL IS REFUSED"* ([row 5b-742](../fragment-issues/section-5b-rows-401-425.md)); its code hosts there |
| **GC15** | GC3 to GC13 on **Revit 2024** and **2027** | The same answers. 2024 is the first release of 64-bit element ids; the tag members `COPY_TAGS_TO_REPLACEMENTS` uses change at 2022 |
| **GC16** | The method stays DRAFT until GC3 to GC13 have been watched working through it, on the owner's word | Nothing to sign by tool: a method has no proof block. `python tools/prove-skill.py --skill replace-in-place-family --plan-only` names what still blocks it |
