# FRAGMENT-ISSUES archive — section 5, rows 1 to 25

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
> Rows 1 to 25 of section 5 belong in this file. A number in that band with no entry below
> was never moved — it is still in the register, in full. Written by
> [`tools/archive-fragment-issues.py`](../../tools/archive-fragment-issues.py).

---

### Row 1

*Moved from the register on 2026-09-23.*

**Defect.** No way to pass a view, category or name. **288 of 308 fragments blocked**, and the refusal said so in its own words

**State.** Fixed — [D-54](../DECISIONS.md)

---

### Row 2

*Moved from the register on 2026-09-23.*

**Defect.** The chain silently dropped what a filter narrowed. `find-untagged-elements` cut 625 to 560 and the next fragment counted 625

**State.** Fixed — identity, not name

---

### Row 3

*Moved from the register on 2026-09-23.*

**Defect.** `refused` read as a finding. **117 fragments**, every write among them, could never pass a negative case

**State.** Fixed — `role: accounting`, [D-52](../DECISIONS.md)

---

### Row 4

*Moved from the register on 2026-09-23.*

**Defect.** `"(null)"` unreadable, so **every creator's** negative case was flagged

**State.** Fixed

---

### Row 5

*Moved from the register on 2026-09-23.*

**Defect.** `deploy-addin.ps1` installed a build for the wrong Revit release. Symptom was *"Revit cannot run the external application"* and nothing else

**State.** Fixed — the script refuses it now

---

### Row 6

*Moved from the register on 2026-09-23.*

**Defect.** `validate` sent writes down the READ path for one commit. Revit refused politely, the fragment reported `refused` like any decline, and it read as intermittent worksharing behaviour

**State.** Fixed — the line carries why

---

### Row 7

*Moved from the register on 2026-09-23.*

**Defect.** **The naming heuristics were dead code.** `provide_role()` answers `"result"` for an entry with no `role:` key, and that default went into the map the judge consults - so every declared name looked explicitly declared, and D-51/D-52's patterns never ran. **102 names across 134 fragments** (`scanned`, `unplaced`, `noConnectors`, `notASheet`) were judged as findings. Found proving `select-scope-boxes`, whose negative had every result at zero and `scanned: 5`

**State.** Fixed - the judge-set and the role-map are separate arguments now

---

### Row 9

*Moved from the register on 2026-09-23.*

**Defect.** **A fragment's OWN risk level was never enforced.** The gate reads the OPERATION's risk from the tool registry — Golden Rule 19, and right — and `run_fragment_write` is declared Modify. So a fragment declaring `risk: ADMIN` or `PUBLISH` ran under a Modify gate and nobody was consulted, though `HeronPermissions` says those *"are not reachable in Phase 0 or Phase 1 at all"*. **12 fragments are above Modify**, and `create-workset` (ADMIN) created one on the first try. The hole existed before `run_fragment_write` and was harmless — no transaction, so nothing could happen. Building the write path made it real

**State.** Fixed — the client refuses to SEND one. Be honest about what that is: a guard against a mistake, not a boundary against malice. A caller skipping this client is unaffected, and Golden Rule 19 forbids closing that by sending the risk over the wire, because then the caller decides how dangerous its own request is

---

### Row 14

*Moved from the register on 2026-09-23.*

**Defect.** **`select-in-region` converts millimetres to feet a SECOND time, so every volume it is given is 304.8x too small.** D-67 put the conversion in the resolver: `OnePoint` returns `MillimetresToFeet(millimetres)` (`RevitFragment.cs:1215`), so an `XYZ` need arrives **already in feet**. `select-in-region` then divides again — `lowMm.X / MillimetresPerFoot` (`fragment.cs:47-48`) — because its header still says *"MILLIMETRES TO FEET BY 304.8, PLAIN ARITHMETIC"*, written when fragments owned the conversion. **Measured, not reasoned:** a box of `-500000,-500000,-500000` to `500000,500000,500000` (1,000,000 mm across, half a kilometre either side of origin) returned `elements 0` against Mechanical Equipment, and its own findings line called the volume *"3281 x 3281 x 3281 mm"* — which is 1,000,000/304.8, the value after ONE conversion, printed in feet and labelled mm. The box actually searched was about **10.7 feet** across. **Both halves of the fragment's own stated safeguard fail together:** it promises *"a value handed in already converted shows up as an absurd volume, not as a quiet zero"* — the value IS handed in already converted, and it produced exactly the quiet zero. **It is the only one.** All 29 XYZ-taking fragments were checked line by line: the other 28 either convert a SCALAR (`spacingMm` in `array-elements`, `gapMm` in `stack-tags`) or use `1.0/304.8` as a one-millimetre tolerance (`move-elements`, `snap-to-grid`, `mirror-elements`), both correct. `create-grid` carries the rule explicitly: *"adding one 'to match the other fragments' would place every grid 304.8 times too far out."* **No signed proof is affected — all 29 are DRAFT**

**State.** **FIXED AND PROVEN, and this cell said OPEN for four days after it was both.** Fixed in `c14b5c5` *"A region was divided by 304.8 twice, so it always found nothing"*. **The fix is not the one this row proposed, and the difference is worth keeping**: the division went, but the report's mm label STAYED - the three report values now MULTIPLY the feet back up, so the sentence can say mm and mean it, and a caller who hands in an already-converted value still sees an absurd volume rather than the quiet zero. Deleting the label as well, as proposed, would have removed the only warning this fragment had. **Proven against a model on 2026-09-13**, signed by Ajmal PS against `Snowdon-scratch_ajmal.al` (9,638 elements, Revit 2024, session 47740): a `200000 x 200000 x 70000 mm` volume returned **1053 element(s)** - the arrangement that returned zero is the one that now answers. **The row's "it is the only one" still holds, re-measured 2026-09-17**: no fragment in the library divides an `XYZ` component by 304.8 or by `MillimetresPerFoot` any more, and this fragment's three remaining uses are all multiplies for the report

---

### Row 15

*Moved from the register on 2026-09-23.*

**Defect.** **A PROOF'S `model:` LINE NAMES THE ACTIVE DOCUMENT, NOT THE ONE THE FRAGMENT RAN AGAINST.** `cmd_validate` records `model` from the opening `count_elements` call, which answers for the document IN FRONT - and then every phase runs against `--in` when a job pins one. The two disagree inside the same proof: `create-callout`, `create-key-schedule` and `duplicate-type` were signed on 2026-09-10 with `model: Project1 work_ajmal.al (3,445 elements)` while each case's own text ends *"on Snowdon-scratch_ajmal.al"*, which is where they actually ran. **The element count is the worst part** - 3,445 belongs to a model those three fragments never touched, so a reader checking the evidence against the model would be checking the wrong one. The evidence itself is sound; only the header lies. It went unnoticed until a job used `in:` for the first time, which is why no earlier proof shows it - every one of them ran against whatever was in front. **The proofs already signed do not need re-running**: the run document is recorded in the case text, so the correction is readable from the file rather than from a repeat.

**State.** **FIXED 2026-09-19 BY THE SECOND ROUTE, which needed no C# and no deploy - this cell said so and was right.** `model_line(opening, phases, revit_version, pid)` in `mcp/client/heron_bridge_client.py` builds the header from where the phases actually RAN, and `cmd_validate` calls it AFTER they have reported instead of before they start. The truth was already in the same file: every phase records `"document": reply.get("document")` from its own reply. **THE COUNT IS DROPPED RATHER THAN CARRIED ACROSS, and that is the half this row calls the worst part.** When the phases name a different document from the opening `count_elements` call, the element count was measured on the model IN FRONT and does not belong to the one the fragment ran against - so the header says so by name rather than attaching a number to the wrong model: *"Snowdon-scratch_ajmal.al, Revit 2024, session 23804 - the count is NOT recorded because 3,445 elements was measured on Project1 work_ajmal.al, the document in front, and this ran against another one"*. **A THIRD CASE TURNED UP IN THE WRITING AND IS NOT IN THIS ROW:** the phases can disagree with EACH OTHER. A proof whose legs ran against different documents is a finding, and picking one of them would hide it - so the header reads `PHASES RAN AGAINST DIFFERENT DOCUMENTS: B, C` and a reader decides. **Six checks in `tests/test_caller_values.py`**, including this row's own case verbatim, and all six run without a Revit because `model_line` is a pure function over what the phases reported. **THE OTHER ROUTE WAS NOT TAKEN AND THIS ROW SAYS WHY** - `count_elements` discards a `document` in silence, so passing one would have changed nothing and looked like it had. **THE PROOFS ALREADY SIGNED STILL DO NOT NEED RE-RUNNING**: the run document is in each case's own text, which is what made this readable from the file rather than from a repeat **AND WHAT THIS CELL SAID WHILE IT WAS OPEN, KEPT BECAUSE IT NAMES THE ROUTE THAT WOULD HAVE LOOKED LIKE A FIX AND WAS NOT ONE:** **OPEN.** Found 2026-09-10. **AND THE OBVIOUS FIX IS A NO-OP - measured 2026-09-17, before it was written rather than after.** The natural repair is to pass the pinned document to the opening call, mirroring `cmd_prove` (`heron_bridge_client.py:1217`, `args["document"] = in_document`). **It would change nothing and look like it had.** `count_elements` does not read the request at all: `RevitOperations.cs:60` is `case "count_elements": return CountElements(app);` - `app` only, no `request` - so a `document` sent with it is discarded in silence and the header would go on naming the active model. The document resolver that DOES honour a name is `RevitFragment.cs:105`, and its own comment says it is absent on every caller that does not send one, *"the proving client"* among them. **So this row needs an ADD-IN change and a deploy, not a client one-liner**: either `count_elements` learns to resolve a document the way the fragment path does, or `cmd_validate` stops sourcing the header from it and builds the header from what the phases already record - each phase carries its own `document` from the reply, which is the truth this row says the header contradicts.

---

### Row 16

*Moved from the register on 2026-09-23.*

**Defect.** **AN EMPTY LIST CANNOT CROSS THE CHAIN, SO A LIST-CONSUMING FRAGMENT CAN NEVER BE GIVEN ITS NEGATIVE CASE.** `Shape` returns null for an empty `IList<ElementId>` (`RevitFragment.cs`, *`return ids.Count == 0 ? null : value;`*), the binder reports *'nothing usable survived'*, and the run stops with `needs_unbound`. **THERE IS NO SUCH RULE FOR A DICTIONARY**, which is how the same night's `group-and-count` bound *"values from read-element-parameters (0)"* and returned `groups 0` perfectly happily. Found proving `describe-blank-parameters`, which consumes `blank` and `absent` from `read-element-parameters`, and the correlation is exact rather than inferred: with `parameterName: Comments` the producer left blank=22 and absent=0, and ONLY `absent` came back unmet; with `System Type` it left blank=0 and absent=0, and BOTH did. **THE RULE IS RIGHT FOR THE SELECTION AND WRONG FOR THE CHAIN, and it is one code path serving both.** An empty SELECTION means nobody picked anything, and refusing is correct. An empty list from a PRODUCER is an answer - *nothing was blank* - and refusing it makes D-30's second leg unreachable for every list-consuming chain fragment, because the negative case is precisely the one that empties the producer. **Could not appear before 2026-09-10**: until row 11 was fixed no value crossed the chain at all.

**State.** **FIXED 2026-09-16 by [row 80](../FRAGMENT-ISSUES.md#), and VERIFIED AGAINST A REAL REVIT by [row 81](../FRAGMENT-ISSUES.md#).** `Shape()` now carries an empty list across as an EMPTY LIST and keeps `null` only for ids that resolved to nothing - exactly the separation this row asked for. Measured on `Snowdon-scratch`, 22 ducts in `FloorPlan: M1`: the binding note reads `blank from read-element-parameters (22); absent from read-element-parameters (0)`, and the reverse on the other leg. **A `(0)` in a binding note was impossible on the day this row was written.** Found 2026-09-10. **This row and [row 79](../FRAGMENT-ISSUES.md#) are ONE defect recorded twice, six days apart, and both still read OPEN on 2026-09-19** - the closure was written in row 80, and no pattern reads a closure written in another row

---

### Row 17

*Moved from the register on 2026-09-23.*

**Defect.** **`remove-parameter-value` calls `ClearValue()`, which Revit allows only on SHARED and PROJECT parameters - so it cannot empty a BUILT-IN one, which is most of what a modeller would want to clear.** `fragment.cs:67` is `parameter.ClearValue();` inside a try/catch that records the exception, and against `Comments` on ten ducts in `Project1 work_ajmal.al` every one came back refused: *"Radius Elbows / Tees (id 925641): Cannot call ClearValue on ..."*, with `cleared 0`, `alreadyEmpty 0`, `missing 0`, `readOnly 0`. **Retried on a single duct Ajmal had filled by hand, to rule out the arrangement: same refusal.** So it is not an empty-parameter problem and not a selection problem - `ClearValue` is simply the wrong call for a built-in parameter, where emptying means setting `""`. Comments, Description and Mark are all built-in. **THE FRAGMENT IS HONEST ABOUT IT**, which is why this is a defect and not a trap: it reported `refused 10` and named every element and Revit's own words, rather than reporting `cleared 0` and reading as a fragment that found nothing to do. Its NEGATIVE leg is sound and was proved in the same run - asked for `HERON_NO_SUCH_PARAMETER` it returned `missing 8` and said *"8 element(s) have no parameter called ..."*. Found 2026-09-12 on the first arrangement the new MODIFY-setup path made possible: `write-element-parameters` fills `Comments` inside the same transaction group, so there was genuinely a value to clear

**State.** **FIXED 2026-09-13 (#138) AND PROVED 2026-09-13 (#139), and NOBODY RE-READ THIS ROW BECAUSE NOTHING COULD SEE IT** - until 2026-09-19 row 17 shared a physical line with [row 16](../FRAGMENT-ISSUES.md#), so `tools/open-defects.py` read this state cell as row 16's and never listed a row 17 at all. **The fix is keyed on `StorageType.String`, not on built-in-ness, and that is the better discriminator**: for a text parameter the empty string IS the empty state, while a length or an airflow still refuses rather than being set to zero - *an empty parameter and a zero are not the same thing* is the whole reason this fragment exists, and a fallback must not quietly break it. The value is read back, not assumed. **The proof is the exact case this row called impossible**: `Comments` - a built-in text parameter - on four walls, `cleared 4`, where this row measured `cleared 0` and ten refusals. Found 2026-09-12

---

### Row 22

*Moved from the register on 2026-09-23.*

**Defect.** **`snap-to-grid` TREATS ITS SPACING AS FEET, SO A 600 mm CEILING GRID IS A 183 METRE ONE — AND IT REPORTS SUCCESS.** `fragment.cs:43-47` computes `Math.Floor((at.X - anchor.X) / spacingX)` and `anchor.X + (cellX + 0.5) * spacingX` where `at.X` and `anchor` are Revit internal FEET — `anchor` because the add-in's point parser converts every XYZ, `at` because that is what a Location returns. **`spacingX` and `spacingY` are plain `double`s, which the binder passes through UNTOUCHED**, so the 600 a modeller types stays 600 and is used as 600 feet. The file knows the unit it is in: `fragment.cs:24` is `var tolerance = 1.0 / 304.8;   // one millimetre, in feet`. **The guard at line 28 passes anyway** — `600 > 0.00328` is true — so nothing refuses.

**State.** **FIXED 2026-09-13 (#138) AND RE-PROVED IN THE SAME COMMIT, which is the part worth reading.** `spacingX` and `spacingY` are divided by `MillimetresPerFoot` before anything uses them, with D-71's rule written over them - *the add-in converts an XYZ at the boundary and cannot convert a bare double, so the conversion belongs here, once, before the value is used for anything*. **THE OLD PROOF IS WHAT SHOWS THE DEFECT WAS REAL**: it recorded `spacingX 600` and `snapped 70`; the proof that replaced it in the same commit records `spacingX 1.96850393700787` - 600 / 304.8 - and `snapped 2`. A proof describing code that no longer runs was REPLACED rather than kept, which is [row 69](../FRAGMENT-ISSUES.md#)'s own demotion rule applied to the change that introduced it. Found 2026-09-13 by Codex review of PR #136

---

### Row 23

*Moved from the register on 2026-09-23.*

**Defect.** **`set-section-mark-visibility` READS AN UNPLACED VIEW'S SHEET NUMBER AS "ON A SHEET", SO IT HIDES NOTHING AND SAYS NOTHING.** `fragment.cs:48-54` reads `BuiltInParameter.VIEWER_SHEET_NUMBER` and sets `onSheet[name] = !string.IsNullOrEmpty(sheetNumber)`. **Revit fills that parameter with its placeholder `---` for a view that is on no sheet**, which is not empty, so every view reads as placed. Line 103 then does `if (onSheet[name]) continue;` and every mark is skipped. Run on 2026-09-13 against `1 - Mech` of `Project1 work_ajmal.al` with `onSheetsOnly: true`: **`markers 5`, `unmatched 0`, `hidden 0`, `shown 0`** — it found all five marks, matched all five to their views, and acted on none. **Those five are four elevations and the owner's section**, confirmed by category count in the same view: `Elevations` returns 4 and the section is the fifth `OST_Viewers` element. So the section drawn specifically to test this was seen, matched, and left visible. **`find-unplaced-views` on the same model the same minute reports `placedCount 0` across 18 views, and `list-sheets` reports 0 sheets.** So the model has no sheets at all and the fragment believed every view was on one.

**State.** **FIXED AND PROVED 2026-09-19 - see [row 122](../FRAGMENT-ISSUES.md#).** The repair does not look for the `---` placeholder at all; it asks Revit which views a viewport points at, so there is no string to recognise and none to translate. Measured on the same model and the same view this row was found on: `hidden 5` with no view on a sheet, `hidden 4` with one placed, against the `hidden 0` recorded here. **The section the owner drew for this is one of the five**, and it is now acted on. Found 2026-09-13 proving it on the owner's own model, after he drew a section specifically so this could be tested

---

### Row 24

*Moved from the register on 2026-09-23.*

**Defect.** **A `ViewSheet` ACCEPTS VIEW-GRAPHICS WORK THAT IS MEANINGLESS ON IT, AND THREE FRAGMENTS REPORT SUCCESS FOR IT.** Run on 2026-09-13 against sheet **A101** of `Project1 work_ajmal.al`, each handed the same 9 ducts that live in `1 - Mech` and are not on that sheet at all: `set-view-crop` returned **`applied true, enclosed 9, viewRefused false`**; `isolate-elements` returned **`isolated 9, viewRefused false`**; and `hide-elements` had already returned **`hidden 191, viewRefused false`** on a sheet in Snowdon ([row 20](../FRAGMENT-ISSUES.md#)). **Each of the three declares `viewRefused` as exactly this guard and a sheet trips none of them.** `isolate-elements` asks `view.CanUseTemporaryVisibilityModes()` — its own comment calls that *"Revit answering for itself"* — and a sheet answers yes. `set-view-crop` sets `CropBoxActive` and reads it back; on a sheet that read comes back true.

**State.** **REPAIRED IN ALL THREE AND PROVED IN ALL THREE - see [row 123](../FRAGMENT-ISSUES.md#), 2026-09-19.** Measured both directions on `Project1 work_ajmal.al`: the off-sheet elements are rejected and the on-sheet one is not, except for `set-view-crop`, which refuses the view because a sheet has no crop region to refuse anything narrower. **The three proofs went STALE and the three fragments were demoted to DRAFT on the owner's word.** This row's own sentence is the fix: *each of the three declares `viewRefused` as exactly this guard and a sheet trips none of them*. Each now refuses a `ViewSheet` first. **The three tests that let a sheet through were each right about a different question** - `CanUseTemporaryVisibilityModes()` asks whether temporary modes work, `CanBePrinted` asks whether a view is drawable, and a sheet honestly answers yes to both. Neither asks *can this view show the elements I was handed*, and nothing in the API does. **`hide-elements`, `isolate-elements` and `set-view-crop` are all PROVEN and their proofs now describe code that no longer runs** - [row 69](../FRAGMENT-ISSUES.md#)'s demotion rule applies to all three and no tool will say so ([row 124](../FRAGMENT-ISSUES.md#)). Found 2026-09-13 after Ajmal made a sheet specifically so these two could be given a view that refuses

---

### Row 25

*Moved from the register on 2026-09-23.*

**Defect.** **A SINGULAR `Element` NEED AT `source: request` CANNOT BE BOUND BY ANY ROUTE, AND THE REFUSAL SENDS YOU DOWN ONE THAT DOES NOT EXIST.** Twelve DRAFT fragments declare one - **14 needs across 12 fragments**, every one meaning *this particular element*: `filter-elements-by-type.exemplar`, `align-elements.reference`, `match-element-type.source`, `join-geometry.target`, `trace-connectivity.start`, `measure-distance.first`/`.second`, `measure-available-fall.upstream`/`.downstream`, `select-group-members.group`, `select-by-host.host`, `select-touching.target`, `read-ceiling-grid.ceiling`, `distribute-along-run.run`. **All three doors are shut.** Typed: `OneElement` refuses any need whose name is not type-shaped, deliberately and correctly. Selection: `BindNeeds` offers it only when `IsElementList(type)` is true, and `Element` is not a list - and separately `src == "request"` is excluded from `selectable` before that. Chain: nothing in the library provides a value under any of those names.

**State.** **CLOSED - IT WAS FIXED THE SAME DAY IT WAS FOUND, AND THE ROW SAT OPEN FOR SIX.** Read 2026-09-19 without a Revit. `RevitFragment.OneElement` at `revit/Heron.Revit.Addin/RevitFragment.cs:2742` now takes the word **`selected`**, and its own comment names this row's own list - *"THIRTEEN fragments were unreachable on a sentence that read like a workaround. **Measured 2026-09-13**: align-elements, distribute-along-run, filter-elements-by-type, join-geometry, match-element-type, measure-available-fall, measure-distance, read-ceiling-grid, select-by-host, select-group-members, select-touching, trace-connectivity"*. The same date as this row. **`selected` MEANS EXACTLY ONE**, and the code says why in the terms this row would want: *"Not 'the first of them' - a Revit selection has no order this code may rely on, and picking one of three would be a confident wrong answer wearing a right one's clothes"*. Zero and many are both refused, and both report how many were found. **RE-MEASURED FROM DISK TODAY, AND THE NUMBERS HAVE MOVED:** the library now holds **16 such needs across 14 fragments**, not 14 across 12 - `create-opening` and `place-hosted-family` joined since. **TWELVE ARE REACHABLE** with `--set <need>=selected`: `align-elements`, `create-opening`, `distribute-along-run`, `filter-elements-by-type`, `join-geometry`, `match-element-type`, `place-hosted-family`, `read-ceiling-grid`, `select-by-host`, `select-group-members`, `select-touching` and `trace-connectivity`. **TWO ARE NOT, FOR A DIFFERENT AND ALREADY-RECORDED REASON**: `measure-distance` wants `first` AND `second`, `measure-available-fall` wants `upstream` AND `downstream`, and one selection cannot fill two singular needs - both sides would bind the same element and the fragment would measure a duct against itself. §6's *"the nine that want one particular element"* already carries that correction: *"the two it was wrong about would not have refused - they would have ANSWERED"*. **WHAT IS STILL TRUE OF THIS ROW AND WORTH KEEPING:** the three doors it names are the right three, and two of them are still shut - `OneElement` refuses a typed name for a need that is not type-shaped, and `BindNeeds` still excludes `source: request` from `selectable`. The third door opened, and it opened by adding a WORD rather than by widening either of the others

---
