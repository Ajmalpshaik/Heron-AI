# FRAGMENT-ISSUES archive — section 5, rows 76 to 100

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
> Rows 76 to 100 of section 5 belong in this file. A number in that band with no entry below
> was never moved — it is still in the register, in full. Written by
> [`tools/archive-fragment-issues.py`](../../tools/archive-fragment-issues.py).

---

### Row 87

*Moved from the register on 2026-09-23.*

**Defect.** **`batch-prove` TURNED A REFUSAL INTO A RESULT, AND I REPORTED THE RESULT AS A MEASUREMENT.** The runner asked only `if not os.path.isfile(record_path)` before judging - *the file is there* is not *this run wrote it*. Any record left by any earlier run, session or model was read and judged as if fresh. **MEASURED 2026-09-14 AND IT HAD ALREADY MISLED ME.** `transfer-project-parameters-between-documents` is `risk: ADMIN`, so `validate` refused it and wrote nothing ([row 54](../FRAGMENT-ISSUES.md#)'s gate working exactly as designed). The runner then read a record written at **01:45 the previous day, Revit session 47740**, and reported `POSITIVE EMPTY (bound, regrouped)` with a finding - `clashed 1 [Sub-Discipline (already bound here, left alone)]` - that I passed on to the owner as that afternoon's result. It was the day before's, on a different session. **THE FIX IS TO DELETE THE RECORD BEFORE THE RUN**, which makes the existing check honest rather than adding a second one: no file means nothing ran, which is what `DID NOT RUN` is for. Verified by re-running the same job file - the ADMIN fragment now reports **`DID NOT RUN`** naming the risk gate, where minutes earlier it had reported numbers. **THE SHAPE IS THE ONE THIS REPOSITORY KEEPS MEETING:** an absence and a stale answer look identical unless something insists on freshness - the same root as [row 79](../FRAGMENT-ISSUES.md#)'s empty-versus-unresolved and [row 75](../FRAGMENT-ISSUES.md#)'s ids that resolve to nothing. **A runner that cannot tell "nothing happened" from "something happened once" will eventually launder one into the other.**

**State.** Fixed. `test_batch_prove` passes. The stale finding I quoted is withdrawn

---

### Row 88

*Moved from the register on 2026-09-23.*

**Defect.** **TWO CORRECTIONS TO MY OWN BLOCKER LIST, AND A HONEST STOP.** [Row 84](../FRAGMENT-ISSUES.md#) recorded `export-views-to-dwg` as *wrote no files*, which reads as a defect. It is not: the fragment **refuses an empty `setupName` on purpose**, and says why - *"Revit's defaults produce a file that opens perfectly and fails the recipient's CAD standard, which is the worst kind of wrong because nothing looks broken"*. It needs a **named DWG export setup saved in the model**, and neither model has one. Content, not a fault, and the refusal is the fragment being right. **`find-unused-materials` AND `find-unused-families` CANNOT BE TRACKED ACROSS THESE TWO MODELS EITHER.** Both report **`unusedMaterials 60`** - `test projject` and `Snowdon-scratch` alike, because both descend from the same Revit template. `scannedInstances 3565` confirms it read the right document. Two models, one number: D-53 needs the answer to MOVE with the input, and it does not move here. They need a model from a different template, not merely a second model. **WHERE THE WORK STANDS: 291 proved, 69 left, and the batches have stopped paying.** The last four rounds returned 1, 2, 1 and 2 passes for 23 jobs, and every failure was one of the blockers already written down. What remains is not more arranging - it is two code changes (the links fix, the graphics input), one permission, and two decisions about how a fragment that answers in words or reports state rather than work should be judged.

**State.** Not a defect row. The list corrected, and the reason for stopping said out loud

---

### Row 94

*Moved from the register on 2026-09-23.*

**Defect.** **THE OWNER OPENED A SECOND PROJECT AND ASKED FOR `switch-active-project` TO BE TRIED PROPERLY. IT WAS, FOUR WAYS, AND REVIT REFUSES ALL FOUR.** [Row 90](../FRAGMENT-ISSUES.md#) retired this capability on one measurement and he was right to push back on that - one refusal can be a wrong call rather than a wall. So it was tried again with two projects **genuinely open**, `test projject` in front and `Snowdon-scratch_ajmal.al` behind, and every attempt answered with the same sentence, which is Revit's own: ***"Changing the active view is not applicable to inactive documents."*** (1) `RequestViewChange` on the host's `UIDocument` - refused. (2) `RequestViewChange` on a `UIDocument` **constructed for the target** - refused; this was row 90's fix and it is still the right code, it just cannot help. (3) the **`ActiveView` SETTER** on that same target `UIDocument`, a different call with a different rule - refused, word for word. (4) naming a view **already open** in the target, in case the restriction was about opening rather than about the document - refused. **THE FALLBACK CODE WAS REMOVED AGAIN RATHER THAN KEPT.** A try/catch whose second branch never succeeds is dead code that reads like a safety net; what is kept is the MEASUREMENT, written into the fragment's own header where the next person meets it before they spend an afternoon. **`ShowElements` WAS DELIBERATELY NOT TRIED.** It can raise a modal message box when it cannot find a view, and a modal dialog inside the bridge's API context hangs the session until somebody clicks it - a risk to his open model for a route that would still be Revit choosing the view. **WHAT IS AND IS NOT BROKEN.** Everything this fragment is built out of works and was checked: path-before-title matching, two matches refused rather than guessed, already-there reported as success, and its own honesty that a request is not a confirmation. **The operation itself is not offered by the API.** It cannot be proved by any arrangement, and it is sitting in the DRAFT count as though it were work outstanding. **Whether it stays in that count is the owner's call, not a tool's** - it is one of the 63.

**State.** Not a defect. 4 routes measured, 1 capability confirmed unreachable

---

### Row 95

*Moved from the register on 2026-09-23.*

**Defect.** **`prove` AND `validate` CHOSE A REVIT SESSION BY TAKING THE LOWEST PID, AND NEVER SAID WHICH.** Both read `live[0]` from the session list with no way to name one, while `cmd_fragment` went the other way and looped over **every** live session. With two Revits open — `PIPE` and a second model — `validate` ran the whole arrangement against the wrong one and returned **`positive ok`** on a phase whose own accounting read **`scanned 0`**. **That is the failure this project exists to refuse:** a green proof taken against a model that never held the arrangement. Not a Revit defect and not a fragment defect — the client picked for you and stayed quiet about it

**State.** **FIXED 2026-09-16.** `--session <pid>` on `fragment`, `prove` and `validate`. `only_session()` **refuses** when more than one session is live and none is named, rather than guessing; `pull_session()` pins the chosen one. The refusal is the point — the old behaviour's whole problem was that it always had an answer

---

### Row 96

*Moved from the register on 2026-09-23.*

**Defect.** **`binds:` IS HONOURED BY THE PYTHON HALF AND NEVER READ BY THE C# EXECUTOR.** `brain/heron_fragment.py:244` (`need_binds`) reads a need's `binds:` key and treats the contract as satisfiable. `revit/Heron.Revit.Addin/RevitFragment.cs` binds by the need's NAME and TYPE and **never opens `binds` at all**, so a fragment relying on it arrives at Revit with the need unbound and stops with `needs_unbound`. **5 fragments cannot be run by any route**, and the two halves disagreeing is what makes it invisible: the shape gate passes, the store indexes it, and only a live run says otherwise

**State.** **FIXED 2026-09-16 - and this cell read `OPEN` for a day after it was closed.** Found 2026-09-16 building the MEP set, **closed 2026-09-15 by the session it names** - the row was filed against a defect another session had already fixed, and the closing sentence was appended without changing the word the reader sees first. `Binds()` in `RevitFragment.cs` now resolves the chain lookup through the alias ([row 100](../FRAGMENT-ISSUES.md#)). - see [row 100](../FRAGMENT-ISSUES.md#), which measured what it actually did: not `needs_unbound` but a silent bind of the Revit selection. The executor now reads `binds`. Two possible fixes were open and they were **not** equivalent — teach the executor to read `binds`, or make the Python half refuse a key the executor cannot honour. The second is safer and smaller; the first is what the contracts were written expecting

---

### Row 97

*Moved from the register on 2026-09-23.*

**Defect.** **`revit_change` REFUSED ITS OWN CHANGE, because one document can be keyed two ways.** `mcp/server/heron_write.py:233` (`key_of`) keys the pinned document by whichever id field the reply happened to carry. An **unsaved** model has no path, so one call keys it by title and the next by session id — two keys for one document — and the pin written by the preview does not match the one the apply looks up. The refusal reads like a permission decline, which sends a reader to the wrong place entirely

**State.** **FIXED - landed as #147 `c7162df`, *"Every write refused against the model it was already pinned to"*.** Confirmed 2026-09-16 by reading the code: `DocumentPin.identity_of` keeps **every** identity field the add-in sent instead of collapsing them, and `_common` compares the most specific field **both** sides carry - *"This is the whole fix. Comparing collapsed keys compares whatever each side happened to win with."* `RevitFragment.Report` now sends `documentPath` and `projectKey` too, so a fragment reply can no longer be the one operation that answers with a title alone. Found 2026-09-16 moving a pipe; the separate session named below (`task_1441f96f`) is the one that finished it. Worked around on the day with `run_fragment_write`, which pins nothing

---

### Row 98

*Moved from the register on 2026-09-23.*

**Defect.** **A READING FRAGMENT CLOSED SIX FAMILY WINDOWS THE OWNER HAD OPEN, AND UNSAVED WORK WENT WITH THEM.** `report-family-tables-in-project` opens every family in the project, reads its lookup table and closes it again — including the ones **already open in Revit's own window**. Six families the owner was part-way through fixing and had not yet loaded back. His words: *"whille your cheking you close all the family i did not lode to the project"*. **A `risk: READ` fragment destroyed work**, and no gate here could have caught it: it opened no transaction and changed no element, so every check reports it clean

**State.** **FIXED 2026-09-16.** The fragment records `app.Documents` **before** it opens anything (`wasOpenBefore`, by title) and never closes a document it did not open — one it found already open is reported in `leftOpen` instead. **The durable rule is not about families:** anything that opens documents closes only what it opened. `risk:` describes what happens to the MODEL, not to the session, and a window somebody is working in is neither

---

### Row 100

*Moved from the register on 2026-09-23.*

**Defect.** **[Row 96](../FRAGMENT-ISSUES.md#) MEASURED: THE EXECUTOR NEVER READ `binds:`, AND WHAT IT DID INSTEAD WAS QUIETLY BIND THE REVIT SELECTION.** Row 96 recorded this as `needs_unbound` and open; that is the symptom when nothing is selected, and it is not the dangerous one. Five fragments declare a need that is filled by a provide under ANOTHER name — `targets` with `binds: elements` (`find-nearest-elements`, `check-minimum-clearance`, `extract-cad-curves`) and `first`/`second` both bound to `elements` (`unjoin-geometry`, `switch-join-order`). `need_binds` at `brain/heron_fragment.py:244` has honoured the key since it existed — composition, the graph and `generate-jobs.py` all read it — and `RevitFragment.cs` looked every need up under its own name. **The reported symptom was `needs_unbound`, and the measured one was worse.** Run on Project1 (3,174 elements, Revit 2020, session 23804) 2026-09-15, `validate find-nearest-elements --setup select-by-categories --keep-chain --set categories=OST_PipeCurves,OST_Walls --set "inViewOnly=FloorPlan: 1 - Mech"`, the reply's own binding note read **`elements from select-by-categories (2); targets from the selection (1)`** and both phases reported `ok`. `targets` was not found in the chain, fell through to the selection branch as the only unbound list of elements, and bound **whatever happened to be left selected in Revit** — one element, with no bounding box, so `targetBoxes 0` and `unmeasurable 2`. A refusal is visible on the page; this is a clean-looking run measuring two pipes against one unrelated leftover. It is exactly the stale-selection failure the proving skill's rule 5 exists for, arriving from the host rather than from the arrangement.

**State.** Fixed — `Binds()` in `RevitFragment.cs` resolves the chain lookup through the alias, the read-back now names it (`targets from select-by-categories as 'elements' (2)`) and the refusal names the name actually looked for. `tests/test_fragment_needs.py` now fails on ANY need key the library uses and the executor never reads, which is the general form of this bug; checked against the pre-fix file, it reports `binds`. Proof: row 97

---
