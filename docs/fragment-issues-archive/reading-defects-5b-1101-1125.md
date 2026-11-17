# FRAGMENT-ISSUES archive — section 5b, rows 1101 to 1125

> **Finished rows, moved out of the live register.** These are rows from section 5b of
> [`FRAGMENT-ISSUES.md`](../FRAGMENT-ISSUES.md) — Heron's own defects found by reading the repository — whose state said they were
> finished when they were moved. Each still has its line in the register: the same number, a
> one-line title, the opening of its state, and a link to its full text here.
>
> **Nothing below was rewritten** except its links, which were re-pointed one folder deeper and
> each checked to reach the same file it reached before. Where a row disagrees with
> [DECISIONS.md](../DECISIONS.md), the [Golden Rules](../14-golden-rules.md) or the
> [Constitution](../../HERON_CONSTITUTION.md), **those win** — a row records what was true on
> the day it was written.
>
> **Nothing new is written here.** A new defect goes in the register, and a row here that turns
> out not to be finished is reopened **there**, on its own line.
>
> Rows 1101 to 1125 of section 5b belong in this file. A number in that band with no entry below
> was never moved — it is still in the register, in full. Written by
> [`tools/archive-fragment-issues.py`](../../tools/archive-fragment-issues.py).

---

### Row 5b-1106

*Moved from the register on 2026-11-17.*

**Defect.** **[`REMOVE_FAMILY_CONNECTOR`](../../brain/fragments/remove-family-connector/fragment.yaml) THREW ON EVERY SUCCESSFUL DELETE: AFTER `doc.Delete` IT READ `c.Id` OF THE DELETED `ConnectorElement` AGAIN - `stillThere` AND `lostOthers` BOTH WALKED THE ELEMENT OBJECTS - AND REVIT ANSWERS *"The referenced object is not valid, possibly because it has been deleted"*.** Met 2026-10-18 proving it on Revit 2020, Family4, a connector made by the run's own `add-family-connector` setup step: positive `fragment_threw`, so the card could never pass a positive. A second target that went with the first would have thrown the same way at `row(c)`.

**State.** **FIXED 2026-10-18 in the card.** ids and words are read before anything is deleted, and the read-back uses the ids alone. Re-run the same day: removed 1, none left; the negative on a plane with no connector removed 0. Signed by Ajmal PS, PROVEN.

---

### Row 5b-1107

*Moved from the register on 2026-11-17.*

**Defect.** **[`ARRAY_FAMILY_FORMS`](../../brain/fragments/array-family-forms/fragment.yaml) DECLARED ITS `labelled` FLAG A RESULT, AND A FLAG BESIDE TWO SENTENCES OUTRANKS THEM (D-51) - SO A ROW OF A TYPED COUNT, `labelled` false, READ AS AN EMPTY POSITIVE WITH TWO COPIES MADE.** Met 2026-10-18: `accept` refused *POSITIVE EMPTY - every declared result came back zero (labelled)* on a run whose `members` named the two copies and `arrayId` the array.

**State.** **FIXED 2026-10-18 in the card.** `labelled` is `role: accounting` - it says how the count was given, not whether anything was arrayed. Same family as row 5b-1105: a read-back or bookkeeping value declared a result. Re-drafted from the same run record, signed by Ajmal PS, PROVEN.

---

### Row 5b-1110

*Moved from the register on 2026-11-17.*

**Defect.** **A LONG PROVING RUN FILLS REVIT 2020'S MEMORY: AFTER ABOUT 110 `batch-prove` JOBS REVIT HELD 28 GB OF COMMITTED MEMORY, THE LAPTOP'S 63 GB COMMIT LIMIT WAS FULL, THE LAST FIVE JOBS CAME BACK `operation_failed: Insufficient memory to continue the execution of the program.` AND THE CLAUDE CHAT DRIVING THEM CRASHED.** Measured 2026-10-18 on the owner's laptop (16 GB RAM), Revit 2020.2 with the add-in deployed from `main` `16ae93d2`, two never-saved scratch documents (Project2 and the Generic Models family Family1): five batches of rolled-back proofs - 111 jobs, each two legs with up to three setup steps - over about two hours. `Get-Process Revit` then read `PagedMemorySize64` 27.6 GB against a working set of 0.9 GB, and `Win32_OperatingSystem` 63.3 of 63.4 GB committed. Read before each of five more jobs by a scratch runner, the same Revit grew 27.68 -> 29.01 GB, about 0.27 GB a job. A fresh Revit (session 2648) went 1.4 -> 10.3 GB over about 35 kept creator runs, three probes and eleven proof jobs, and to 18.3 GB by fifty jobs. Only closing Revit freed it. **Not measured: what holds the memory.** Each fragment run compiles C# inside Revit's process, and on .NET Framework a loaded assembly is never unloaded - a candidate, not a finding. The proofs that ran before the failures are sound; the five that failed were run again on the fresh Revit.

**State.** **MOVED TO ISSUE [#932](https://github.com/Ajmalpshaik/Heron-AI/issues/932) 2026-11-16** - still open, tracked there; this row is kept whole. Was: **OPEN.** Until it is found: read Revit's commit before each batch, and restart Revit (a scratch model needs no save) every 50 to 60 jobs or past about 15 GB.

---

### Row 5b-1112

*Moved from the register on 2026-11-17.*

**Defect.** **[`SET_MEP_JUSTIFICATION`](../../brain/fragments/set-mep-justification/fragment.yaml) ON REVIT 2020 SET NOTHING: BOTH DUCTS ANSWERED *"2 of 2 offset(s) did not take the value"* FOR 50 MM, AND 0 MM ON THE SAME DUCTS COUNTED `set 2`.** Measured 2026-10-18, scratch Project2: a rectangular Supply Air duct and a round Return Air duct, both drawn by `CREATE_DUCT` at 2700 mm, `horizontalOffsetMm 50, verticalOffsetMm 50` -> `set 0`; `0, 0` -> `set 2`. So the positive does nothing on this release, and writing the values already there counts as a change. Not run on 2024. Seen again 2026-10-18 on a project from Revit's Systems-Default_Metric.rte, a `Rectangular Duct: Radius Elbows / Taps` main: `set 0` for 50 mm.

**State.** **MOVED TO ISSUE [#942](https://github.com/Ajmalpshaik/Heron-AI/issues/942) 2026-11-16** - still open, tracked there; this row is kept whole. Was: **OPEN.** Whether the 2020 parameters refuse the write or the read-back compares in the wrong unit is not measured.

---

### Row 5b-1113

*Moved from the register on 2026-11-17.*

**Defect.** **[`PLACE_ROOMS`](../../brain/fragments/place-rooms/fragment.yaml) THROWS ON A LEVEL THAT HAS NO PLAN VIEW - *"level Parameter name: Level is not associated to a plan view"* - WHERE IT SHOULD REFUSE BY NAME.** Measured 2026-10-18, Revit 2020, scratch Project2: `levelId=Level 3` (made by `CREATE_LEVEL`, which makes no plan), `placeKind=rooms`, rolled back. The positive on Level 2 placed one room inside four walls a setup step drew. Revit's `NewRooms2(Level)` needs a plan; the fragment lets the exception out, so the reply is `fragment_threw` and names an argument rather than the fix.

**State.** **MOVED TO ISSUE [#923](https://github.com/Ajmalpshaik/Heron-AI/issues/923) 2026-11-16** - still open, tracked there; this row is kept whole. Was: **OPEN.** Check the level has a plan view before the call and refuse naming the level.

---

### Row 5b-1114

*Moved from the register on 2026-11-17.*

**Defect.** **FIVE CREATORS DECLARE THEIR ONE RESULT AS A SINGLE OBJECT, SO `batch-prove` AND `accept` READ THE POSITIVE AS `POSITIVE UNREADABLE` AND NONE CAN BE PROVED AS WRITTEN: [`CREATE_AREA_PLAN`](../../brain/fragments/create-area-plan/fragment.yaml) (`created` a `View`), [`CREATE_WALL_ELEVATION`](../../brain/fragments/create-wall-elevation/fragment.yaml) (`created` a `View`, `marker` an `Element`), [`CREATE_BEAM_SYSTEM`](../../brain/fragments/create-beam-system/fragment.yaml) (`beamSystem`) AND [`CREATE_FLEX_PIPE`](../../brain/fragments/create-flex-pipe/fragment.yaml) (`created` a `FlexPipe`).** Measured 2026-10-18, Revit 2020, scratch Project1 and Project2: each made its element (`created ViewPlan`, `created ViewSection`, `beamSystem BeamSystem`, `created FlexPipe`) and the judge found nothing it could count.

**State.** **MOVED TO ISSUE [#940](https://github.com/Ajmalpshaik/Heron-AI/issues/940) 2026-11-16** - still open, tracked there; this row is kept whole. Was: **OPEN.** Declare a countable result beside the object - an id list, or a `type: string` result naming what was made - then prove each.

---

### Row 5b-1115

*Moved from the register on 2026-11-17.*

**Defect.** **[`MOVE_ANNOTATION`](../../brain/fragments/move-annotation/fragment.yaml)'S PURPOSE SAYS IT MOVES *"a tag, a dimension, a text note"*, BUT A TEXT NOTE CAME BACK `notMovable` - IT MOVES ONLY BY A TAG HEAD OR A LOCATION POINT, AND A TEXT NOTE HAS NEITHER.** Measured 2026-10-18, Revit 2020, scratch Project2: one `3.5mm Arial` note made by `CREATE_TEXT_NOTE`, `target 1000,0,0` -> `moved 0, byHead 0, byLocation 0, notMovable 1 [345497]`. The same call moved two room tags, and that run is the proof signed the same day. The code has no `TextNote` path, and three of the card's utterances are about text notes.

**State.** **MOVED TO ISSUE [#951](https://github.com/Ajmalpshaik/Heron-AI/issues/951) 2026-11-16** - still open, tracked there; this row is kept whole. Was: **OPEN.** Move a `TextNote` by `Coord` (or `ElementTransformUtils.MoveElement`), or take text notes out of the purpose and the utterances.

---

### Row 5b-1116

*Moved from the register on 2026-11-17.*

**Defect.** **FOUR READ-BACKS WERE DECLARED RESULTS AND WERE FILLED ON A LEG THAT CHANGED OR FOUND NOTHING, SO EACH NEGATIVE READ `NEG NOT EMPTY`: `MOVE_VIEW_CUT_PLANE`'S `newOrigin`, `DIMENSION_ELEMENT_FACES`'S `measured`, `REPORT_SPACE_ENVELOPE`'S `takeoffJson` AND `CHECK_FAMILY_STANDARDS`'S `classificationReport`.** Measured 2026-10-18, Revit 2020: the North elevation moved 0 mm and still filled `newOrigin`; a 3D view refused with *"NOTHING WAS DRAWN"* in `measured`; a Generic Models family with no Spaces still got the JSON skeleton; a project with nothing off standard still counted 43 blank OmniClass Numbers. In each a count beside it held the evidence - `movedMm` 500 against 0, `created` 5 against 0, `spaces` 2 against 0, `offStandard` 189 against 0.

**State.** **FIXED 2026-10-18** - the four entries are `role: accounting` in `fragment.yaml` (outside the proof seal, fingerprints unchanged), each re-drafted from the same run.

---

### Row 5b-1118

*Moved from the register on 2026-11-17.*

**Defect.** **[`PLACE_ROOMS`](../../brain/fragments/place-rooms/fragment.yaml) ON A LEVEL WHOSE TWO ENCLOSED AREAS ALREADY HAVE ROOMS MADE TWO MORE ROOMS, BOTH LISTED IN ITS OWN `unbounded`.** Measured 2026-10-18, Revit 2020, scratch Project1: Level 1 with Room 1 and Room 2 placed and two Spaces in the same areas, `placeKind=rooms`, rolled back -> `created 2 [345804, 345806], unbounded 2 [345804, 345806]`. A modeller asking to fill the gaps on a finished level gets two redundant, unbounded rooms instead of *"every area already has a room"*. Which areas Revit's `NewRooms2` thought were empty is not measured.

**State.** **MOVED TO ISSUE [#923](https://github.com/Ajmalpshaik/Heron-AI/issues/923) 2026-11-16** - still open, tracked there; this row is kept whole. Was: **OPEN.** Delete, or refuse to keep, a room `NewRooms2` places that comes back unbounded, and say none were needed.

---

### Row 5b-1119

*Moved from the register on 2026-11-17.*

**Defect.** **[`PLACE_ACCESSORY_ON_RUN`](../../brain/fragments/place-accessory-on-run/fragment.yaml) ON REVIT 2020 COULD NOT PUT AN ACCESSORY INTO A LONE DUCT OR PIPE: EVERY RUN ENDED *"The duct/pipe has been modified to be in the opposite direction causing the connections to be invalid."* AND WAS ROLLED BACK.** Measured 2026-10-18, scratch Project1 (metric architectural template), each rolled back: `M_Balancing Damper - Rectangular: Standard` at `positionAlong 0.5` on one Supply Air rectangular duct drawn by `CREATE_DUCT` (2 errors on 2 elements); `M_Gate Valve - 50-300 mm: 100 mm` on one Domestic Cold Water pipe drawn by `CREATE_PIPE` (the same, 2 on 2); the damper over five ducts at once (6 on 6). Both families came from Revit 2020's own US Metric library through `LOAD_FAMILY`, and the run's rollback was clean each time. Not run on 2024. Seen again 2026-10-18 on a project from Revit's Systems-Default_Metric.rte, whose duct types carry routing preferences: the template's own `M_Fire Damper - Rectangular - Simple` on a `Radius Elbows / Taps` main, the same refusal (2 on 2).

**State.** **MOVED TO ISSUE [#943](https://github.com/Ajmalpshaik/Heron-AI/issues/943) 2026-11-16** - still open, tracked there; this row is kept whole. Was: **OPEN.** Whether the split pieces are reconnected the wrong way round, or the family is placed facing against the run, is not measured.

---

### Row 5b-1121

*Moved from the register on 2026-11-17.*

**Defect.** **A ROLLED-BACK [`LINK_FILES`](../../brain/fragments/link-files/fragment.yaml) LEAVES THE LINKED FILE OPEN IN REVIT: AFTER EIGHT PROOF JOBS THAT LINKED ONE FILE AND ROLLED BACK, `Application.Documents` HELD THIRTEEN OPEN `HeronLinkSource` DOCUMENTS, AND A LINK THEN KEPT FOR REAL ANSWERED `GetLinkDocument()` = NULL WHILE ITS TYPE READ `IsLoaded` TRUE - SO EVERY LINK READER SAW IT AS NOT LOADED.** Measured 2026-10-18, Revit 2020 session 16356, scratch Project1: batch-prove round 16 ran `LINK_FILES` on `HeronLinkSource.rvt` (the owner's Save As of the test building) as the fragment and as a setup step, every group rolled back; `LINK_FILES` kept once afterwards said *"linked, origin to origin, Loaded."*; a scratch read then found instance 345545 with `GetLinkDocument()` NULL, `RevitLinkType.IsLoaded` true, and thirteen linked documents of that title open. `CHECK_LINK_COORDINATES` answered *"not loaded - not checked"*, `ZOOM_TO_LINKED_ELEMENTS` `shown 0`, `FIND_CLASHES` `clashes 0` against the link's walls, `SYNC_SPACES_WITH_LINKED_ROOMS` `created 0`. Whether Revit or the fragment leaves the document open is not measured.

**State.** **MOVED TO ISSUE [#946](https://github.com/Ajmalpshaik/Heron-AI/issues/946) 2026-11-16** - still open, tracked there; this row is kept whole. Was: **OPEN.** Until it is: prove link readers in a fresh Revit with the link kept once, never after rolled-back link runs, and close Revit after them.

---

### Row 5b-1122

*Moved from the register on 2026-11-17.*

**Defect.** **[`CHECK_LINK_COORDINATES`](../../brain/fragments/check-link-coordinates/fragment.yaml) WRITES *"not loaded - not checked"* INTO ITS `coordinates` RESULT, SO A RUN THAT CHECKED NOTHING READS AS A PASS IN `batch-prove` AND IN `accept`'S EVIDENCE RULE.** Measured 2026-10-18, Revit 2020, twice (rounds 16 and 17): positive `coordinates HeronLinkSource.rvt : 1 : location <Not Shared>: not loaded - not checked.`, `mismatched 0`; the negative (no link, or a family) empty - both judged PASS. Neither was signed.

**State.** **MOVED TO ISSUE [#946](https://github.com/Ajmalpshaik/Heron-AI/issues/946) 2026-11-16** - still open, tracked there; this row is kept whole. Was: **OPEN.** Leave `coordinates` empty for a link that was not checked (or count the links checked as the result), and say *"not checked"* in `findings`.

---

### Row 5b-1124

*Moved from the register on 2026-11-17.*

**Defect.** **[`LINK_FILES`](../../brain/fragments/link-files/fragment.yaml) MAKES A LINK INSTANCE THAT NEVER REACHES ITS DOCUMENT: ON REVIT 2020 ITS `GetLinkDocument()` STAYS NULL AND NOTHING OF THE LINKED MODEL IS DRAWN, WHILE THE SAME FILE LINKED BY HAND (Insert > Link Revit) DRAWS AND READS AT ONCE - AND ITS PROOF, SIGNED THE SAME DAY, WAS WITHDRAWN.** Measured 2026-10-18, Revit 2020 session 30308, a fresh scratch Project1 and the owner's `HeronLinkSource.rvt`, no rolled-back link run first: `LINK_FILES` kept once answered *"linked, origin to origin, Loaded."* and instance 345447 read `GetLinkDocument()` NULL with its type `IsLoaded` true; a screenshot of the Revit window showed the Level 1 plan empty. The owner then linked the same file by hand: instance 345450, the same `RevitLinkType`, read `GetLinkDocument()` = HeronLinkSource with 6 walls, and the building appeared. The fragment calls `RevitLinkType.Create` then `RevitLinkInstance.Create(doc, typeId, placement)` in one transaction and reads back only the type's `GetLinkedFileStatus()` - so *Loaded* proved nothing about the instance. That read-back is what the signed proof rested on; the card is back to `main`'s, DRAFT. Row 1121's thirteen open documents after rolled-back runs are this fragment's runs too.

**State.** **MOVED TO ISSUE [#946](https://github.com/Ajmalpshaik/Heron-AI/issues/946) 2026-11-16** - still open, tracked there; this row is kept whole. Was: **OPEN.** Find why the instance does not attach (create the type and the instance in separate transactions; `RevitLinkType.Load`), then read back `GetLinkDocument()` and one element of the link before saying *linked*.

---

### Row 5b-1125

*Moved from the register on 2026-11-17.*

**Defect.** **[`SYNC_SPACES_WITH_LINKED_ROOMS`](../../brain/fragments/sync-spaces-with-linked-rooms/fragment.yaml) MADE A SPACE FOR EACH LINKED ROOM, BUT EVERY SPACE READ BACK WITH NO AREA - AND THE RUN STILL COUNTS THEM IN `created`, SO IT JUDGES AS A PASS.** Measured 2026-10-18, Revit 2020 session 30308, scratch Project1 holding no walls of its own and `HeronLinkSource.rvt` linked by hand (Insert > Link Revit, origin to origin, the instance that reads its document - row 1124), Room Bounding ON on both link types (`SET_LINK_ROOM_BOUNDING`), rolled back, twice: *"room 1 'Room': space made on Level 1"*, *"room 2 ..."*, `created 2`, and *"space 345472 reads back with NO AREA - placed where nothing bounds it"*. `batch-prove` judged PASS both times; neither was offered for signing. Whether linked walls bound host spaces inside the rolled-back group at all is not measured.

**State.** **MOVED TO ISSUE [#921](https://github.com/Ajmalpshaik/Heron-AI/issues/921) 2026-11-16** - still open, tracked there; this row is kept whole. Was: **OPEN.** Leave a space that reads back with no area out of `created` (count it apart), then prove it on a kept, room-bounding link.

---
