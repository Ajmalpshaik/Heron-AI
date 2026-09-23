# Needs checking — Group E

> One group of [the register](../NEEDS-CHECKING.md), in its own file since 2026-09-23 so that it can be read
> alone. **The register's rules, and every group's place in it, are on that page.** A new row
> for this group goes in this file. [`tools/needs-checking-register.py`](../../tools/needs-checking-register.py)
> reads it back into the register for every tool that reads the register, so a row here is seen
> exactly as it was seen there. Written by
> [`tools/split-needs-checking.py`](../../tools/split-needs-checking.py).

## Group E — the refusals

Each of these is a rail. A rail that has never been tested is decoration.

| ID | Do this | Pass looks like |
|---|---|---|
| **E1** | Preview, wait **over 2 minutes**, then approve | Refuses as **expired**. Nothing moves |
| **E2** | Preview, draw one more duct, then approve | Refuses — **the model moved on**. Names both counts |
| **E3** | Preview, click into a **second open project**, approve | Refuses, names **both** models, and says the approved one is **still open** |
| **E4** | Preview, then **close** the first project, approve | Refuses and says it was **CLOSED** — a different message from E3. Golden Rule 20 treats these differently |
| **E5** | E4 again on a model that has a **link loaded** | Still says closed. Proves the `IsLinked` filter — a loaded link must not count as "still open" |
| **E6** | Approve **twice** in a row | The second finds nothing to approve. It must **not** move them a further 200 mm |
| **E7** | Pin a duct, then preview | Reported as **skipped**, and left alone after the move |
| **E8** | On a **workshared** model with a duct owned by another user, preview | Reported as skipped, and the move does not fail because of it |
| **E9** | Open a dialog in Revit, then ask to move | *"Revit is busy"* — a clean refusal, not a hang. Recovers by itself |
| **E10** | Put two ducts in a **Revit group**, then ask to move that category up 200 mm | They are reported as **did NOT move at all**, by count, with the words *"almost certainly inside a group"* — and the ungrouped ones still move. **This is the case Revit will not tell you about**: `MoveElements` returns normally and moves nothing for a group member, no exception and no warning, so counting "it did not throw" as "it moved" reports a clean success for elements that have not shifted a millimetre. Proved against a real model in the owner's earlier work; Heron now compares positions either side instead of trusting the call. A group member is **not** pinned, so `E7`'s skip filter does not catch it |
| ~~**E11**~~ | With Project1 unsaved and never saved, run a read tool (`revit_select_by_category`), then… — [full row](../needs-checking-archive/group-e.md#row-e11) | It runs. |
| ~~**E12**~~ | Run `revit_change` in Project1, click into a second open project, ask for the same change again RUN… — [full row](../needs-checking-archive/group-e.md#row-e12) |
| ~~**E13**~~ | The same, but click into a family editor rather than a second project RUN 2026-09-15 - PASSED.… — [full row](../needs-checking-archive/group-e.md#row-e13) |
| ~~**E14**~~ | In a fresh chat, make `revit_change` the first thing asked RUN 2026-09-15 - PASSED. Pin before… — [full row](../needs-checking-archive/group-e.md#row-e14) |
| ~~**E15**~~ | `revit_select_by_category`, then `revit_change`, same model, no switching RUN 2026-09-15 - PASSED… — [full row](../needs-checking-archive/group-e.md#row-e15) |

### E11-E15 were RUN on 2026-09-15 and the queue kept asking for them until 2026-09-16

**Struck 2026-09-16. Nothing about them was re-run — only the mark was wrong.**

The session that ran them struck the **`Do this` cell** and left the **ID** alone. A reader
sees a crossed-out instruction and correctly reads *done*. `tools/owner-queue.py` only ever
looks at the ID — `^\|\s*(~~)?\*\*([A-Z]\d+[a-z]?)\*\*` — so for eight days it printed all
five back to the owner as work still waiting on him, beside the genuinely open `E16`-`E18`.
**Four of the five had PASSED.**

This is the same shape as the drift this file records about its own prose totals, one layer
down: **a mark a person reads and a mark a tool reads, kept in two places, updated in one.**
The prose drifts were caught by running the count; this one was invisible to the count, because
the count was the thing that was wrong.

- `E11`, `E13`, `E14`, `E15` — **PASSED**, verbatim results in each row.
- `E12` — **FAILED**, and closed rather than passed. Its re-run after the fix is **`E16`**,
  which is open. Per this file's own rule, *a struck row is not automatically a passed one*.

**If you strike a row, strike the ID.** Anything else is a comment.

### What has been observed so far, and it is NOT any of E11-E15

**2026-09-15, Revit 2020 session 8084, model `PIPE` (3,287 elements, on the
owner's Desktop), add-in deployed from `claude/friendly-hypatia-196de4`.** One
model was open and it was the owner's own file, so no row above could be run:
E12 needs a second project, E13 needs a family, and E14/E15 write for real.
**No row above is ticked.**

> **Renumbered 2026-09-16.** This paragraph said *"E11 needs a second project,
> E12 needs a family, and E13/E14"* — the numbering from before #147's row
> became `E11`. See the correction under [`E12 FAILED`](#e12-failed-and-the-cause-is-that-projectkey-is-not-a-key)
> below. The observation itself is unchanged and still stands.

What WAS exercised is the **add-in half**, directly over the bridge, read-only.
Four `run_fragment_read` calls of `count-elements` against the same model at the
same moment, differing **only** in `expectProject` - which is what makes it a
contrast rather than four separate observations:

| `expectProject` | Result |
|---|---|
| omitted entirely | ran (the proving client's own path - it sends no key) |
| `""` | ran, reached need-binding - `needs_unbound` on `elements`, a LATER stage |
| the real key, `8764c510-...-0000c160` | ran, reached need-binding - the same later stage |
| `00000000-dead-beef-...` | **REFUSED** - `wrong_document`: *"the one this would have run in is PIPE. NOTHING was read."* |

So the guard is **deployed and live**; **empty really does mean do not check**,
which is E13's mechanism exercised rather than reasoned about; and **a matching
key really does pass** - worth its own row, because a guard that refused
everything would also have refused the wrong key, and the two would be
indistinguishable from the refusal alone. The sentence says *read* rather than
*written* because it was on the read path.

**And the half that makes the guard reachable a second time:** a SUCCESSFUL
fragment run (`list-levels`) now answers with `documentPath` and `projectKey`,
and that key is **byte-identical** to `count_elements`' on the same model. That
is E14's root cause measured as fixed - the reply used to carry the title alone,
so `DocumentPin` fell to its `title:` fallback, `project_key` stayed `None`, and
`expectProject` went out EMPTY on every later call, switching the guard off for
exactly the chat that needed it.

Error replies report `projectKey = None`, correctly rather than as a gap: they
return from `Json.Error` and never reach `Report`.

### E12 FAILED, and the cause is that `ProjectKey` is not a key

> **This heading said `E11` until 2026-09-16, and every E number in the three sections
> above it was one too low.** The rows were renumbered on `main` when #147's unsaved-model
> row became `E11` and pushed the four below it down; the prose was written before that and
> was merged without being re-read against the table it describes. [`the 2026-09-16 sitting`](../handover-archive/2026-09-16-e11-e14-renumbered-mid-flight.md)
> warned about exactly this — *"If you remember E11 failed, that row is E12 now"* — and the
> warning did not reach the file it was warning about. **The numbers below are corrected to
> match the table; the fact that they were wrong is left here on purpose**, because a
> register whose prose and whose rows disagree is the failure this whole file exists to catch.

**Observed 2026-09-15, Revit 2024.3 session 2924, two blank projects and a
family, add-in deployed from `claude/friendly-hypatia-196de4`.**

The guard compares `RevitOperations.ProjectKey(doc)`, which is
`doc.ProjectInformation.UniqueId`. **That value is inherited from the template**,
so it does not identify a project at all:

| Document | `projectKey` |
|---|---|
| Project1 (Revit 2024) | `8764c510-57b7-44c3-bddf-266d86c26380-0000c160` |
| Project2 (Revit 2024) | `8764c510-57b7-44c3-bddf-266d86c26380-0000c160` |
| `PIPE.rvt` (Revit **2020**, a different model in a different release) | `8764c510-57b7-44c3-bddf-266d86c26380-0000c160` |
| the family | `None` |

So `here != expectProject` is **false between any two projects made from the
same template**, and the write goes through. E13 passes only because `None` is
genuinely different; E15 passes for a reason that is indistinguishable from the
bug.

**This is not fixed by tightening the comparison.** There is nothing to tighten:
both sides are equal and both are wrong.

### The direction the owner asked for, and it is already half-built

Asked on the day: *"if I open project 1 and I tell it to pin the model, you have
to work only in project 1 and refer to project 2, and I move to project 3 -
Heron needs to work on project 1... that is agentic work."*

That inverts the question from *"did the user move?"* to *"which model was I told
to work on?"*, and it does not depend on a key at all.

**`RevitFragment.Run` already accepts it.** `RevitFragment.cs:105` reads a
`"document"` argument, finds that document among the open ones by title, and
refuses with `no_such_document` listing what is open. `revit_change` simply never
sends one, so the add-in falls back to the active document.

**Demonstrated the same day, end to end, with the FAMILY in front the whole
time:** `list-levels` was run against Project2 by name to read the owner's level
(`ajmal testing level @ 12500 mm`), then `create-levels` was run against Project1
by name to add ten levels following it - `ajmal testing level - 01` at 15000 mm
through `- 10` at 42000 mm, verified by reading back (`created 10 item(s)`,
`nameRefused 0`, Project1 3 -> 13 levels, **Project2 untouched at 5**). Every
reply reported `wasActiveDocument = False`.

**The remaining question was ambiguity, not mechanism, and it now has a rule.**
Two open models can share a TITLE, which is the case `DocumentPin` was built
around.

### Done in code the same day, and NOT yet run against Revit

`revit_change` now sends `document` (the pinned title) and `documentPath` (the
pinned path) with every write, so the add-in works on the model the chat was
pointed at instead of falling back to the active one. `RevitFragment.Run`
resolves the path FIRST - unique among open models - and the title second.

**A title that matches TWICE is refused**, `ambiguous_document`, rather than
letting the last document round the loop win. That silent tie-break was already
there and is the worst answer available on a path about to commit: it cannot be
told from a correct one, and which model it picks depends on the order Revit
hands its documents back.

`no_such_document` and `ambiguous_document` were both added to the failure
table. `no_such_document` PREDATES all of this and was never in it - the same
gap `wrong_document` had, sitting unnoticed until its sibling was added beside
it. Both return above the transaction group, so both are REFUSED rather than
an unknown outcome that tells the user to go and check the model.

`expectProject` stays. It is no longer the mechanism, and it still catches the
family case (E13) and any two models whose keys genuinely differ.

**E16 to E18 below are what this owes.** Nothing in this section has met Revit:
the owner was working in Revit 2020 when it was written, and it was not
deployed.

| ID | Do this | Pass looks like |
|---|---|---|
| **E16** | Repeat **E12** exactly - pin to Project1, click into Project2, ask for the same change | The change lands in **Project1**, the model that was named. Not a refusal any more: the write is AIMED, so moving the screen is no longer an error to report. Check Project1 gained it and **Project2 did not** |
| **E17** | Pin to a project, **close it** in Revit, then ask for a change | `no_such_document`, naming what IS open, and nothing written. Heron must not open a project by itself, and must not fall back to whatever is in front |
| **E18** | Open **two** models both called `Project1` (one per Revit session, or a detached copy), pin one, ask for a change | `ambiguous_document` - it refuses rather than picking one. **Save one of them and repeat:** the paths now differ, so it must resolve cleanly and write into the right one |

### E19-E24 - a fragment write answers Revit's failures itself, and a move skips what changed in central (2026-09-23)

**Built in the cloud, compiled on all eight releases, and NEVER RUN IN REVIT.** Package C3 of the
[earlier-brain plan](../work-notes/plans/earlier-brain/02-work-packages.md).

Until this change a fragment write - every capability `revit_change` runs - had **no Revit failure
handling at all**: whatever Revit posted at commit went to Revit's own handling, and Revit's handling
of an ERROR is a resolution, one of which can be to **delete the elements the error names**. Every
transaction on that path now carries a preprocessor that dismisses and counts a WARNING, rolls the
whole job back on ANYTHING else, and quotes Revit's words in the answer
([`RevitFragment.cs`](../../revit/Heron.Revit.Addin/RevitFragment.cs) `Discipline`, the rule in
[`HeronFailureNote.cs`](../../revit/Heron.Revit.Addin/HeronFailureNote.cs)). The move's pre-check
([`RevitWrite.cs`](../../revit/Heron.Revit.Addin/RevitWrite.cs) `Partition`) now also skips an element
changed or deleted in central since this copy last reloaded, and says which reason applied.

**What the cloud proved, and it is not these rows:** the rule itself, run 40 ways without Revit
(`tests/test_failure_note.py`); that every transaction on the path is put under it before its script
runs (the same suite, reading the code); that every Revit member called exists on 2020-2027
(`tools/check-api-surface.py`). **Whether Revit then does what it is told is only answerable here.**

**Every row runs on a DETACHED COPY first** ([Golden Rule 18](../14-golden-rules.md)) - E21 and E22 exist
to make Revit want to delete something - **and re-reads what was written afterwards**: an element
count cannot see an edit ([FRAGMENT-ISSUES §1c](../FRAGMENT-ISSUES.md)). E23 and E24 need a **workshared
central with two local copies**.

| ID | Do this | Pass looks like |
|---|---|---|
| ~~**E19**~~ | ~~A write fragment arranged so Revit raises a **WARNING** at commit, run **with** apply - for example two elements of one category given the same Mark. **Confirm by hand first** that Revit shows it as a warning, not an error~~ | **No dialog.** The change is kept; the verdict ends *"Revit raised N warning(s) while this ran, and Heron dismissed them..."* quoting Revit's words; the reply's `warnings` is N and `warningsDismissed` lists them; **one** Ctrl+Z undoes the whole job. A dialog appearing is a FAIL - it means Revit's handling was reached - **PASSED 2026-09-23 on the owner's PC** - Revit 2024 (session 3544), model `Heron-C3-test`, a new project from his template saved in `D:\Ajmal\Heron-Proving`; add-in built from main `173a4524` and deployed to 2020, 2024 and 2027, the three installed. **By hand first:** the owner gave both walls the Mark `TEST` and Revit kept it (re-read, 2 of 2) - his words: *"revit will give the wanring that is duplicat vale but we can ingore that"*. Then `heron_bridge_client.py fragment write-element-parameters --write --apply --set parameterName=Mark --set value=HERON-E19` on the two walls: *"APPLIED 'write-element-parameters' was KEPT ... Revit raised 1 warning while this ran, and Heron dismissed it rather than let a dialog stop the job: "Elements have duplicate "Mark" values." (2 elements). Dismissing a warning removes the message, not its cause"* - the singular is the code's form for N = 1; `warnings` 1, `warningsDismissed` one entry, that text, times 1, elements 2. Re-read: 2 of 2 walls carry HERON-E19, and Revit's own warning list still holds the warning (`read-model-warnings`, 1). No dialog (owner). **One Ctrl+Z:** both Marks empty, 0 of 2 HERON-E19 and 0 of 2 `TEST` - only Heron's job came back - 4 grids, 3,416 elements as before |
| ~~**E20**~~ | ~~E19 **without** apply~~ | *"NOTHING WAS KEPT"*, the same warning sentence, no dialog - and the value E19 wrote is **not** in the model when re-read - **PASSED 2026-09-23 on the owner's PC** - the same walls, `fragment write-element-parameters --write --set parameterName=Mark --set value=HERON-E19`, no apply: *"ROLLED BACK  NOTHING WAS KEPT. 'write-element-parameters' ran for real and Revit reported the transaction group rolled back afterwards ... Revit raised 1 warning while this ran, and Heron dismissed it rather than let a dialog stop the job: "Elements have duplicate "Mark" values." (2 elements)..."*; `applied` false, `rolledBack` true, `warnings` 1. Re-read: 0 of 2 walls carry HERON-E19, both Marks empty, no warning stored, 3,416 elements. No dialog (owner). The rollback cleared Revit's selection, as the proving notes have long said |
| ~~**E21**~~ | ~~A write fragment arranged so Revit raises an **ERROR** at commit. Candidate: lock a dimension between two grids, then move one grid - check by hand first that Revit calls it an error and offers a resolution (a *Remove Constraints* style button), which is exactly the resolution this must never apply~~ | `operation_failed`, quoting Revit's error in its own words, and *"Revit confirmed the rollback"*. **Nothing deleted and nothing unlocked:** the element count, the grids' positions and the lock are all as before. No dialog. **A missing lock or a missing element is the failure this row exists for** - **PASSED 2026-09-23 on the owner's PC** - grids A and B 6000 apart (`CREATE_GRIDS`), grid B pinned (`SET_PIN_STATE`), and the owner drew the A-B dimension and locked it. **By hand first**, moving grid A: Revit's own box, *"Error - cannot be ignored / Constraints are not satisfied"*, with **Remove Constraints**, OK greyed and Cancel - he pressed Cancel (his screenshot). Then `fragment move-elements --write --apply --set offset=500,0,0` on grid A: `operation_failed` - *"'move-elements' ran, and Revit would not keep it. Revit would not let it through: "Constraints are not satisfied" (3 elements). Heron rolled the whole job back rather than let Revit resolve it its own way - Revit's own fix for an error can be to delete the elements it names. Revit confirmed the rollback, so the model is exactly as it was."* Re-read: 3,418 elements as before, the dimension still there, `report-constraints` still finds the one constraint holding A and B (grids 1 and 2 free), grid B still pinned. No dialog, and the dimension still 6000 and locked (owner, screenshot). **The positions were checked by eye, not read:** no proven read reports where a grid is - `report-location` finds no point and no box on one |
| ~~**E22**~~ | ~~E21's write as a **setup step** in front of any fragment~~ | `setup_failed`, naming the step and saying the fragment under test never ran; the model as before, re-read - **PASSED 2026-09-23 on the owner's PC** - grid A selected, then `heron_bridge_client.py validate --out <scratch file> list-grids --write --setup move-elements --set offset=500,0,0`. `move-elements` is MODIFY, so the client sends it in the request's `setup` array and the add-in runs it inside the fragment's own group: `setup_failed` - *"Revit would not keep the arrangement step 'move-elements', so the fragment under test never ran. Revit would not let it through: "Constraints are not satisfied" (3 elements, in setup step 'move-elements'). Heron rolled the whole job back rather than let Revit resolve it its own way ... Revit confirmed the rollback, so the model is exactly as it was."* Re-read as E21: 3,418 elements, the dimension, the constraint and the pin all as before. No dialog, the dimension 6000 and locked (owner) |
| **E23** | Workshared: in copy **B** move one duct and Synchronise. In copy **A**, not reloaded, turn on Worksharing Display - *Model Updates* - and wait until that duct shows as updated in central: the status is a locally cached value that Revit refreshes on its own schedule, so previewing the moment B synchronises can read it as current and prove nothing. Then preview *"move ducts up 200 mm"*. Then delete a duct in B, Synchronise, wait the same way, and preview in A again | That duct is **skipped** with the reason: the add-in's `skipReasons` and `summary` say *"1 changed in central since this model last reloaded (Reload Latest, then ask again, to include it)"*, then *"1 deleted in central"*. **After Reload Latest in A** the changed duct is included. The chat's own line still says *"pinned, or owned by another user"* until FRAGMENT-ISSUES 5b-158 is fixed - read the reply, not only the chat - **NOT RUN 2026-09-23 - the owner deferred it**, once its purpose and its cost (two Revits, a test central, half an hour) had been explained: Revit's own borrowing error would stop such a move anyway, and E21 shows Heron then cancels cleanly. Ready for it: `D:\Ajmal\Heron-Proving\E23-E24\Snowdon-E23-central.rvt`, a plain copy of Revit 2024's own *Snowdon Towers Sample HVAC*, which is not workshared, waiting to be made the test central. **Found while preparing it:** the chat's preview line prints only a count and *"pinned, or owned by another user"* (5b-158), and the command line has no preview command - so `skipReasons` and `summary` can be read only by sending `preview_move` through the client library itself |
| **E24** | E23's preview on the largest workshared category to hand (a thousand elements or more), timed, and the same preview on a **detached, non-workshared** copy of the same model | **Record both times.** Revit's own documentation says `GetModelUpdatesStatus` returns a *"locally cached value"*, so it should not reach the central server and the two should be close; a workshared preview markedly slower says otherwise, and the plan asked for exactly this number - **NOT RUN 2026-09-23 - deferred with E23.** Its plain copy is ready beside it, `D:\Ajmal\Heron-Proving\E23-E24\Snowdon-E24-detached.rvt`. The preview reads every element of its category in the whole model (`RevitWrite.Partition`), and accepts ducts, pipes, pipe fittings and pipe accessories - so the largest category is one of those four |

**Run on the owner's PC, 2026-09-23 - the four rows about the fragment write path PASSED, the two about central were deferred.** Revit 2024, the test project `D:\Ajmal\Heron-Proving\Heron-C3-test.rvt`, the add-in built from `main` at `173a4524` and deployed on all three installed releases with the three Heron DLLs hash-checked against the build; a wrapper around the client's own `main()` kept each raw reply. What Revit posts at commit now reaches Heron and not Revit's own handling: a duplicate Mark was dismissed and quoted, a broken constraint rolled the whole job back with Revit's words, and nothing was deleted, unlocked or moved. The evidence is in each row.
