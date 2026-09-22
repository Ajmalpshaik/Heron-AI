# FRAGMENT-ISSUES archive — section 5, rows 26 to 50

> **Finished rows, moved out of the live register.** These are rows from section 5 of
> [`FRAGMENT-ISSUES.md`](../FRAGMENT-ISSUES.md) — Heron's own defects found by proving — whose state said they were
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
> Rows 26 to 50 of section 5 belong in this file. A number in that band with no entry below
> was never moved — it is still in the register, in full. Written by
> [`tools/archive-fragment-issues.py`](../../tools/archive-fragment-issues.py).

---

### Row 26

*Moved from the register on 2026-09-23.*

**Defect.** **`create-mep-system-type` DECLARES TWO RESULTS AND NEITHER CAN EVER BE READ AS A QUANTITY, so it cannot pass the runner however well it works.** Run on 2026-09-13 against `Project1 work_ajmal.al` it did exactly the right thing in both legs — positive: `created` = **`MechanicalSystemType`**, `classification` = **`SupplyAir`**, with `refused` carrying *"'HERON_TEST_SYSTEM' was copied from 'Supply Air' and is clas..."*; negative, asked to copy from `ZZZNOSUCHSYSTEM`: `created` **null**, `classification` **empty**, and *"no system type called 'ZZZNOSUCHSYSTEM'"*. **A person reading that has a complete proof.** The runner cannot: `created` returns the OBJECT, which reports as the literal text `MechanicalSystemType`, and naming `classification` with `expect:` fails the same way — *"classification SupplyAir cannot be read as a quantity"*.

**State.** **FIXED 2026-09-13 (#139) BY A JUDGE RULE, NOT BY TOUCHING THE FRAGMENT** - which is why the implementation still reads exactly as this row describes it, and why re-reading the fragment would suggest nothing had changed. `heron_validate._result_appeared` asks whether a declared result is PRESENT in the positive and GONE in the negative: D-30's comparison, for a result that is a THING rather than a count. **This row's own measurements are the case it was built for** - `created` = `MechanicalSystemType` positive, `created` null negative. `create-mep-system-type` is PROVEN, marked in the same commit the rule landed in. **[Row 41](../FRAGMENT-ISSUES.md#) still lists this among its four and is stale for it.** Found 2026-09-13

---

### Row 27

*Moved from the register on 2026-09-23.*

**Defect.** **`create-wall` BUILDS AT 3000 FEET WHEN ASKED FOR 3000, AND IT IS `PROVEN`. A LENGTH SCALAR HAS NO UNIT RULE IN THIS LIBRARY AND THE TWO HALVES DISAGREE.** Built on 2026-09-13 in a fresh model (`test projject`, Revit 2024) with `height=3000`, meaning 3 m. It returned `created 4 item(s)` and `findings [4 wall(s) built on 'Level 1' at **914400 mm** high, unconnected]`. **Read back from a different fragment minutes later**, `report-bounding-box` on the same four walls answered `combinedSizeMm` **`8200 x 6200 x 914400 mm`**, `maxZ 3000`, `minZ 0`, bound `elements from the selection (4)`. The plan footprint is exactly right — 8000 + 200 of wall thickness — so the `XYZ` run converted correctly and **only the scalar did not**. 3000 × 304.8 = 914400: the height was used as FEET. `impl/any/fragment.cs:61` passes `height` straight to `Wall.Create` and line 75 multiplies it by 304.8 *for the message*, so the fragment reports the wall it actually built and the sentence is true. **THE CONTRACT SAYS NOTHING.** `height: double, source: request` carries no unit and no comment; the impl header says *"Lengths are internal FEET"*; the caller-facing side says the opposite — `OnePoint` is documented in MILLIMETRES and the add-in's own refusal for a bad `double` reads *"Type digits only - 250 or 250.5, not 250mm"*.

**State.** **FIXED 2026-09-13 — [D-71](../DECISIONS.md).** Ajmal was asked and answered: **a number a caller types is millimetres, always.** 32 values across 23 implementations now convert once, at the top, before first use; `check-fragments-compile` is clean on all eight releases. The one refusal is `select-by-numeric-parameter.tolerance`, which compares against whatever parameter was named and cannot know its unit — its own header already said so. **Seventeen `PROVEN` fragments changed code and are now `RE_PROVE`**: their proofs measured a version that no longer runs. Found on the first build of the owner's "let Heron make what it needs" method, and found **only because the thing built was read back** — nothing in the proof that promoted `create-wall` ever measured a wall

---

### Row 29

*Moved from the register on 2026-09-23.*

**Defect.** **`set-wall-constraints` COULD NEVER DO THE ONE THING ITS FLAG EXISTS FOR, AND REPORTED IT AS A REFUSAL RATHER THAN A FAULT.** Run 2026-09-13 on `test projject` against four plain walls - base `Level 1`, unconnected top, 3000 mm - asked for top `Level 2` with `allowUnconnectedToBecomeBound` **true**: `rehosted 0`, `refused` holding all four, `notAWall 0`, `unconnectedTop 0`, and both levels resolved (`newBase Level`, `newTop Level`), so the blanket refusal at `fragment.cs:44` had not fired. **One run isolated it.** The same four walls with the flag **false** - which skips the top block entirely - came back **`rehosted 4`, `refused 0`, `unconnectedTop 4`, `elevationChanged 0`**. The base path was never the problem. **The cause is the order of two lines**: the top block read `WALL_TOP_OFFSET` and tested `IsReadOnly` *before* setting `WALL_HEIGHT_TYPE`, and on a wall with an unconnected top Revit greys that offset out because there is no top level to measure from. So every unconnected wall was refused - and an unconnected wall is exactly what `allowUnconnectedToBecomeBound` is there to convert.

**State.** **FIXED 2026-09-13.** The constraint is set first and the offset read after. Same command, same model: `rehosted 4`, `refused 0`, `elevationChanged 0`. Found by proving it - the POSITIVE came back empty, which is the half D-30 is not written about

---

### Row 30

*Moved from the register on 2026-09-23.*

**Defect.** **`create-schedule` CRASHED AT THE EXACT POINT IT WAS WRITTEN TO REFUSE POLITELY.** `fragment.cs:39` called `ViewSchedule.CreateSchedule(doc, categoryId)` and `fragment.cs:41` tested `schedule == null` to produce *"Revit declined to create a schedule for that category - not every category can be scheduled"*. **Revit THROWS instead of returning null**, so that sentence was unreachable. Run 2026-09-13 on `test projject`: `categoryId=Ducts` created one with both fields and `missingFields 0`; `categoryId=Views` came back **`fragment_threw`** - *"'create-schedule' threw while running: categoryId is not a valid category for a regular schedule. Parameter name: categoryId"*. A fragment that throws where it means to refuse has no negative leg at all.

**State.** **FIXED 2026-09-13.** The call is wrapped and Revit's own sentence is passed on, because "not every category can be scheduled" does not say WHICH rule was broken and Revit's does. The negative now reads `created (null)`, `refused "...Revit said: categoryId is not a valid category for a regular schedule"`. Found the same way as row 29

---

### Row 31

*Moved from the register on 2026-09-23.*

**Defect.** **`place-rooms` REPORTED EVERY ROOM IT MADE AS "NOT ENCLOSED", INCLUDING THE ONES SITTING CORRECTLY INSIDE FOUR WALLS.** Run 2026-09-13 on `test projject` against a **7200 x 5200 x 3000 mm box built on Level 2 and read back from a separate process at Z 4000 to 7000** - a genuinely closed region with no room in it - it answered `created 1`, **`unbounded 1`**. The same call on Level 1, whose only enclosed region already holds a room, answered `created 1, unbounded 1` as well. **Two opposite arrangements, one answer.** The cause is a read-back with nothing to read: `fragment.cs` took `BuiltInParameter.ROOM_AREA` in the same breath as `NewRooms2`, and **a room has no area until Revit regenerates** - so the zero-area test, which the file correctly calls *"exactly the 'Not Enclosed' condition"*, was measuring Revit's not-yet rather than the room's geometry.

**State.** **FIXED 2026-09-13** with one `doc.Regenerate()` before the loop. Same command, same model: Level 2 now `created 1, **unbounded 0**`, Level 1 still `created 1, unbounded 1`. **This is the opposite result to [row 18](../FRAGMENT-ISSUES.md#)**, where a Regenerate between a write and its read-back changed nothing and the hypothesis was correctly discarded - so "add a Regenerate" is not a rule, it is a thing to test each time

---

### Row 33

*Moved from the register on 2026-09-23.*

**Defect.** **`create-sheet` CRASHED WHERE IT MEANT TO REFUSE - THE SECOND FRAGMENT OF THIS EXACT SHAPE IN ONE AFTERNOON.** `fragment.cs:68` called `ViewSheet.Create(doc, titleblock)` and tested `sheet == null` to say *"Revit declined to create the sheet"*. **Revit throws instead.** Run 2026-09-13 on `test projject`: `titleblockTypeId = "A1 metric: A1 metric"` made the sheet (`created 1`); a **Detail Item** family type came back `fragment_threw` - *"The ElementId titleBlockTypeId does not correspond to a TitleBlock type. Parameter name: titleBlockTypeId"*. Identical to [row 30](../FRAGMENT-ISSUES.md#), found the same day on a different fragment, which is what turned it from an incident into a pattern worth searching for.

**State.** **FIXED 2026-09-13**, the same way: the call is wrapped and Revit's own sentence is passed on. The negative now reads `created (null)`, `refused "Revit declined to create the sheet. Revit said: The ElementId titleBlockTypeId does not correspond to a TitleBlock type"`. **Both fixes were only possible because the id could be typed in at all** - `titleblockTypeId` and `categoryId` were both unreachable until [row 28](../FRAGMENT-ISSUES.md#) was fixed that morning

---

### Row 35

*Moved from the register on 2026-09-23.*

**Defect.** **`create-flex-duct` REPORTED THE LENGTH OF THE ROUTE IT REFUSED TO BUILD, beside a `created` of null.** Its contract says `lengthMm` is *"how long the flex actually came out, which is the number the length limit is about - the answer, not a count of it"*, and it is declared `role: result`. Run 2026-09-13 on `test projject` with a 9000 mm route against a 3000 mm limit: `created (null)`, `refused` correctly saying *"The route measures 9000 mm, over the 3000 mm limit given, so NOTHING WAS CREATED"* - and **`lengthMm 9000`**, which reads as a 9000 mm flex duct existing. The gate caught it as *"the negative case returned content instead of nothing"*. **The author had already got this right one branch earlier**: the empty-point case at `fragment.cs:55` does `lengthMm = 0.0;`, and the over-length branch three lines below simply did not.

**State.** **FIXED 2026-09-13**, one line in each of the two branches that refuse after measuring. The route's real length is not lost - it is in the refusal sentence, which is where a measurement of what was ASKED FOR belongs rather than in the field that says what was MADE. Now `created (null)`, `lengthMm 0`. Found by proving the fragment

---

### Row 37

*Moved from the register on 2026-09-23.*

**Defect.** **A REVISION CANNOT BE NAMED, so `revisionId` is receivable in type and unresolvable in practice.** [Row 28](../FRAGMENT-ISSUES.md#)'s `OneIdNamed` maps `revisionId` to `typeof(Revision)` and resolves it through `ElementsNamed`, which matches on `Element.Name`. **A Revision has no name a modeller would recognise.** Tried 2026-09-13 on `test projject`, which holds two revisions: `"HERON TEST REVISION"` (the description), `"Seq. 2"` and `"2"` all came back *"No revision called ... in test projject"*. `list-revisions` on the same model the same minute reports them as **`Seq. 1 - Revision 1`** and **`Seq. 2 - HERON TEST REVISION`** - but that string is the fragment's own formatting, not `Element.Name`. So `add-revision-cloud` is still unreachable, one layer further in than it was this morning.

**State.** **WITHDRAWN 2026-09-13 by [row 46](../FRAGMENT-ISSUES.md#), and this cell still said `OPEN` on 2026-09-16.** Row 46 measured it: a Revision's `Element.Name` **is** `"Seq. N - Description"`, that string is Revit's own, `OneIdNamed` resolves it as it stands, and `set-sheet-revisions` was proved through this very route the same hour. **No rebuild was ever needed.** The reasoning below is left standing because it is a good example of a sound-looking inference from three refusals that a single measurement overturned. What identifies a revision to the person holding the drawing is its **SEQUENCE NUMBER** - it is the first column of Revit's own Sheet Issues/Revisions dialog, and `delete-revision` already takes `sequenceNumber: int` rather than a name, which is the same conclusion reached independently. `OneIdNamed` should resolve `revisionId` by sequence, falling back to description. **The general lesson is worth more than the row**: adding a CLASS to the dispatch is not the same as making its instances reachable, and the only way to tell is to ask for one. Found proving `add-revision-cloud`

---

### Row 38

*Moved from the register on 2026-09-23.*

**Defect.** **`set-view-phase` THREW AWAY A REFUSED PHASE FILTER WHENEVER THE PHASE ITSELF WENT THROUGH, and reported the view as a clean success.** `fragment.cs` ended each view with `else if (refused && !touched) templateControlled.Add(view.Id);` - and `touched` goes true the moment the PHASE is written. So a view whose phase set and whose phase FILTER was refused in the same call satisfied `!touched == false`, was added to nothing, and came back with `phaseFilterSet` silently one short. **Measured 2026-09-13 on `test projject`**, view `1 - Mech`, with `HERON PLAN TEMPLATE` holding the Phase Filter and the phase free to change: **`phaseSet 1`, `phaseFilterSet 0`, `alreadySet 0`, `templateControlled 0`, `unsupported 0`, `findings 0`.** Nothing anywhere said the filter had not taken. **`alreadySet` being empty is what rules out the innocent reading** - "it was already that filter" has its own branch at `fragment.cs:129` and adds to `alreadySet`.

**State.** **FIXED 2026-09-13** by deleting `&& !touched`. A refusal is a refusal whether or not the OTHER property went through; the read-back already caught it and only the reporting was discarding it. The same call now answers `phaseSet 1`, `templateControlled 1 [312]` and a finding. **The finding's wording was corrected too**: it claimed a view template was holding the property, which is much the commonest reason and not the only one, so it now says a view listed here had SOMETHING refuse and names the template as where to look first. Found proving the fragment against the case `tests/cases.yaml` calls its first

---

### Row 40

*Moved from the register on 2026-09-23.*

**Defect.** **`place-schedule-on-sheet` HAS A POSITIVE AND NO REACHABLE NEGATIVE, because every route that can supply its `elements` supplies only schedules.** Its own refusals are real and there are five, at `fragment.cs:36, 47, 57, 69, 76`. **None can be triggered from a caller.** *Not a sheet* (:36) is unreachable because `OneIdNamed` resolves `sheetId` among `ViewSheet` only, so a wrong name is a `bad_request_value` from the binder before the fragment runs - measured 2026-09-13 with `sheetId = "ZZZ NO SUCH SHEET"`. *Not a schedule* (:47) needs a floor plan in the selection, and the only fragment that provides schedules is `find-schedules`, which returns nothing else; `find-views` excludes them the other way - `skippedNonDrawing 8`. *A schedule TEMPLATE* (:57) is excluded by `find-schedules` too. And **the obvious fallback is not a refusal either: Revit placed the SAME schedule on the SAME sheet twice without complaint** - `placed 1 [926190]` then `placed 1 [926191]`, `refused 0` both times.

**State.** **CLOSED BY [row 85](../FRAGMENT-ISSUES.md#) 2026-09-15 - the negative arrived by a route this row did not consider, and it was [row 80](../FRAGMENT-ISSUES.md#)'s empty-list fix that opened it.** `placed 1` onto sheet `M002 'Notes, Symbols & Schedules'` against `placed 0` for a name nothing matches, and the negative's binding note reads **`elements from find-schedules (0)`** - an empty list crossing the chain, which was impossible on the day this row was written. **So the missing negative was never *hand it something that is not a schedule*; it was *hand it no schedule at all*** - and this row ruled that route out by looking only at what could supply a WRONG element. `place-schedule-on-sheet` is PROVEN. The original reading, kept because it is still sound: the fragment is not at fault and its positive is sound: `HERON DUCT SCHEDULE` on `HERON TEST SHEET` gave `placed 1` with `placedAt` naming the schedule, the sheet and the position. What is missing is a way to hand it something that is NOT a schedule, and that is a gap in the ARRANGEMENT MECHANISM rather than in the code: `validate` runs one setup chain for both phases, so a negative needing a different KIND of element cannot be built. **This is the fifth time today Revit allowed something expected to refuse** - see rows 24, 34, 36 and the `create-sheet-list` name clash. Leaving it DRAFT was the honest outcome on the day, and it stayed DRAFT for exactly two days

---

### Row 42

*Moved from the register on 2026-09-23.*

**Defect.** **`set-category-visibility` COUNTED THE CALL IT MADE, NOT THE CHANGE IT CAUSED - so hiding something already hidden reported success.** `fragment.cs:46-47` was `view.SetCategoryHidden(category.Id, !visible); changed++;` with no read-back and no already-that-way branch. **Measured 2026-09-13 on `test projject`**: Walls hidden in `{3D}` and kept, then asked to hide Walls in `{3D}` again - **`changed 1`**, `notControllable 0`, `refused 0`. A caller checking whether their instruction did anything was told yes while nothing moved. Same shape as [row 20](../FRAGMENT-ISSUES.md#)'s `hidden = ids.Count`, in a second fragment.

**State.** **FIXED 2026-09-13.** `GetCategoryHidden` is read before and after and only a real change is counted; a category that was already the way it was asked for goes to a new `alreadyThatWay`, declared `role: accounting` because it is never a thing this call did. **The distinction is the point, not the number**: `changed 0` on its own cannot separate "already done" from "something is wrong", and the sibling fragments all name it - `set-element-phase.alreadySet`, `apply-view-template.alreadyOnIt`, `change-element-type.alreadyThatType`. Same call now: `changed 0, alreadyThatWay 1`

---

### Row 43

*Moved from the register on 2026-09-23.*

**Defect.** **`remove-parameter-value` COULD NOT CLEAR A PLAIN TEXT PARAMETER - its main path failing on its commonest case.** The file's own header says *"WRITE_ELEMENT_PARAMETERS CANNOT DO THIS... Clearing is its own call and **works on all four**"*. It does not. `fragment.cs:67` calls `parameter.ClearValue()` and **Revit refuses it for a built-in text parameter**. Measured 2026-09-13 on `test projject`: `write-element-parameters` put `Comments = "HERON TEST"` on four walls (`written 4`), and clearing the same field answered **`cleared 0`, `alreadyEmpty 0`, `readOnly 0`, `refused 5`** - every wall reporting *"Cannot call Clear..."*. **The fragment was not lying** - it reported the failure as a refusal rather than counting it - but the premise in its header was wrong, and Comments is the field anybody actually wants emptied.

**State.** **FIXED 2026-09-13**, and the fix was already written five lines above the bug: *"an empty string clears a text parameter"*. A `StorageType.String` parameter that refuses `ClearValue` now falls back to `Set(string.Empty)`, **read back before it counts**. `cleared 0` became `cleared 4` on the same call. **ONLY for String**: a length, an airflow or one pointing at another element still refuses rather than being set to zero, because *"an empty parameter and a zero are not the same thing"* is the reason the fragment exists and a fallback must not quietly break it

---

### Row 44

*Moved from the register on 2026-09-23.*

**Defect.** **THIRTEEN SIGNED FRAGMENTS WERE STILL `DRAFT`, SO THE NEXT ROUND OFFERED THEM TO BE PROVED AGAIN - AND THE OWNER FOUND IT, NOT A TOOL.** `heron_validate.py accept` writes the proof and deliberately never writes `heron-status`; its own header says so, and the separation is right - the evidence and the decision to trust it are different acts. Promotion is therefore a HAND EDIT, and a hand edit gets forgotten. A forgotten one is invisible from the inside: the file still reads `DRAFT`, so `grep -h '^heron-status:'` counts it as unproved and it returns to the queue. Ajmal saw it from the only place it shows - *"becose i signed item i have to do again i sow lkike that for exambple set view crop i singe multiple time"* (2026-09-13). Measured the same hour: 13 fragments carried `by: Ajmal PS` at `DRAFT`, the oldest signed 2026-09-08, none with a caveat saying it was held back on purpose. **The published count was wrong too** - README said 247 PROVEN / 113 DRAFT when six of that 113 were already signed. **THE THIRTEEN SPLIT IN TWO, AND MERGING THEM WOULD HAVE HIDDEN BOTH.** `can_promote` was asked about each rather than the answer being re-derived: **six** were pure waste - signed, fingerprint fresh, nothing blocking, promoted on the spot. **Seven** were STALE: the implementation changed under them after signing (the D-71 units work), so their proofs no longer describe the code that would run and re-proving them is CORRECT. D-30 wants a stale proof to be loud. That is [D-52](../DECISIONS.md)'s rule in another costume - a count of things correctly refused is never evidence of something found - and the fault was never telling him which kind he was looking at.

**State.** Fixed - `tools/check-signatures.py`. **This cell said *"wired into `gates.yml`"* until 2026-09-16 and it was never true**: the commit that adds the step sits on the local branch `ci/run-check-signatures` and **cannot be pushed** - the `gh` token has no `workflow` scope. It runs in CI only because it **rides inside `tools/check-docs.py` as section 9**, which `gates.yml` does call. The distinction matters the day somebody edits that workflow by hand: the section's own comment says to give it a real step and delete the section. Exit 1 on an UNUSED signature, stale ones reported and not failed. Proved both ways before it was trusted: with `set-view-crop` put back to `DRAFT` it names it and exits 1; with the six promoted it exits 0

---

### Row 46

*Moved from the register on 2026-09-23.*

**Defect.** **ROW 37 IS WRONG, AND NO REBUILD IS NEEDED: A REVISION IS NAMED `"Seq. N - Description"`, AND THAT STRING IS REVIT'S OWN.** Row 37 tried `"HERON TEST REVISION"`, `"Seq. 2"` and `"2"`, got three refusals, saw `list-revisions` print `Seq. 2 - HERON TEST REVISION`, and concluded **that string is the fragment's own formatting, not `Element.Name`** - so `OneIdNamed` would need rebuilding to resolve by sequence number. **The one string it never tried was the whole one.** Measured 2026-09-13 on `test projject`: `revisionIds="Seq. 2 - HERON TEST REVISION"` resolved and `set-sheet-revisions` reported `sheetsChanged 2`; `revisionIds="Seq. 1 - Revision 1"` resolved the same way, `sheetsChanged 2`, so it is a rule and not one lucky string. `Element.Name` on a `Revision` IS the sequence and the description joined, built by Revit. **WHAT WENT WRONG WAS THE INFERENCE, NOT THE TESTING.** Row 37 tried the description, the prefix and the bare number - three guesses at what a name might be - and never tried COPYING WHAT THE MODEL HAD JUST PRINTED. It then explained the failure with a theory ("that is the fragment's formatting") and promoted the theory to a conclusion strong enough to schedule an add-in rebuild against. An untested explanation of a failure is not a finding; it is the next thing to test. **WHAT THIS REOPENS:** `add-revision-cloud` was recorded unreachable in three places (§3i, §6 and row 37) on this reasoning alone. It is worth trying with the full name before any of that is believed. `delete-revision` taking `sequenceNumber: int` is still a good contract and nothing here argues against it.

**State.** Fixed by measurement - row 37's conclusion is withdrawn, the rebuild it asked for is not needed, and `set-sheet-revisions` was proved through this route the same hour

---

### Row 47

*Moved from the register on 2026-09-23.*

**Defect.** **`accept` COULD NOT SIGN A D-53 TRACKING PROOF AT ALL, SO THE ROUTE FOR FRAGMENTS WITH NO EMPTY CASE WAS QUIETLY SHUT.** `draft` has understood tracking since D-53 - it reads `record["tracking"]` and writes the negative case from it - but `_evidence_refusal`, the guard `accept` added 2026-09-12, never looks at that key. It demands a negative PHASE that `looks_empty`, and a fragment that cannot come back empty has neither. Measured 2026-09-13 on `report-filterable-parameters`, which is one of them: any category it is handed is either filterable or is reported as unfilterable, so one result is always non-zero. Both shapes were refused and **both refusals blamed the fragment** - drop the negative phase and it says *"the run record has no negative phase in it"*; keep it and it says *"the negative case came back with content"*. Neither mentions tracking. `count-elements` and `report-category-visibility` are PROVEN by this route and were signed before the guard existed, so nothing was visibly broken until the next one came along. **THE BAR ADDED IS THE VARIATION, NOT THE ROW COUNT** - rows that all carry the same value are exactly what a fragment ignoring its input produces, so identical rows are refused and fewer than three are refused. Proved both ways before trusting it: the real 6-row set (69, 74, 60, 48, 29, 0) is accepted; the same set with every value forced to 69 is refused; two rows are refused. **AND A SECOND GUARD, FROM ROW 44'S COMPLAINT RATHER THAN FROM A CRASH.** `accept` writes the draft's fingerprint through unchanged, so signing a draft taken against code that has since changed lands a proof that is stale on arrival: `can_promote` refuses it for ever, the fragment stays DRAFT, and the next round offers it up again. That is the exact loop Ajmal reported. `check-ceiling-coordination` was sitting in that state - draft `c905e58bb9224d02`, code now `4db71c18b96ed9bb` - and passed every other check. A wasted signature is now refused BEFORE the name is typed.

**State.** Fixed - `_evidence_refusal` in `brain/heron_validate.py`. `test_validate_agent`, `test_batch_prove` and `test_fragment_store` all pass

---

### Row 48

*Moved from the register on 2026-09-23.*

**Defect.** **`read-space-loads` WAS ONE SIGNATURE AWAY FROM BEING PROVEN ON A RUN THAT READ NOTHING - AND SIXTEEN DRAFTS ARE WAITING BEHIND IT.** Its draft's positive case read 5 spaces on `Snowdon-scratch` and returned **`heatingW 0`, `coolingW 0`, `airflowLs 0`** - every figure the fragment exists to report, empty. It passed every check because `noLoad`, a count of the spaces Revit REFUSED to give figures for, is declared `role: result`, and 5 of those carried the positive leg past the judge. That is [D-52](../DECISIONS.md) word for word: a refusal count is never evidence something was found. `noLoad` is now `accounting` - which does not make it unimportant, `notSpaces` beside it is equally worth acting on and has always been accounting - and the draft correctly reads `POSITIVE EMPTY` at once. **Proving this fragment needs a model whose spaces have been ANALYSED; Snowdon's have not**, so no arrangement on that model would have worked. This is [row 41](../FRAGMENT-ISSUES.md#)'s family, ninth member. **THE REASON IT SURFACED IS WORTH MORE THAN THE ROW.** Asking `_evidence_refusal` about every draft on disk - read-only, signing nothing - turned up **16 of 72 that would be accepted**, only 4 of them from that day's proving. Twelve valid drafts had been sitting unsigned across earlier rounds while the library counted their fragments as unproved. Two of the sixteen were wrong on inspection: this one, and `select-by-insulation`, which looked identical in both legs until the run record showed `elements 1053` bare against `elements 0` insulated - a textbook pass whose giveaway numbers were locals the summary had truncated. **Read the run record, not the summary line.**

**State.** Fixed - role corrected, and the draft-audit is worth re-running before every signing round

---

### Row 50

*Moved from the register on 2026-09-23.*

**Defect.** **A FRAGMENT THAT MAKES ONE THING RETURNS THE THING, NOT A COUNT, AND THE SIGNING GATE COULD ONLY COUNT.** `positive_worked` skips any declared result that `_is_helper_object` recognises - the rule exists for real working values like `OverrideGraphicSettings`, and it is right to. But that test is a heuristic on the RENDERED string, and it cannot tell a working object from a created one: both arrive as a bare CamelCase word. Measured 2026-09-13 proving `create-mep-system-type`, which duplicates a duct system type. Positive: **`created MechanicalSystemType`, `classification SupplyAir`**. Negative, given a name the model has not got: **`created (null)`, `classification ""`**. Both real results were discarded as helper objects and the leg read `POSITIVE UNREADABLE - nothing it returned is a declared result that can be read as a quantity`. **THE SAME HEURISTIC READ THE NEGATIVE CORRECTLY** - `(null)` and `""` count as zero - so only the positive was ever mis-read, and the evidence was sitting in plain sight on both lines. **THE FIX ASKS THE QUESTION D-30 ACTUALLY ASKS, WHICH IS A COMPARISON RATHER THAN A COUNT.** When the positive reads UNREADABLE, `_result_appeared` now looks for a declared result that is PRESENT in the positive and GONE in the negative. That is stronger than counting one leg, not weaker: a fragment succeeding while doing nothing returns the same value in both phases and cannot pass it, and the `OverrideGraphicSettings` the original rule guards against is present in both legs and cannot pass it either. Proved three ways before it was trusted - the real record accepted; the negative faked to carry the same object refused; the positive faked to create nothing refused. Re-auditing all 72 drafts afterwards unlocked exactly one, the fragment that found it, so it is not quietly waving others through.

**State.** Fixed - `_result_appeared` in `brain/heron_validate.py`. `test_validate_agent` and `test_batch_prove` pass

---
