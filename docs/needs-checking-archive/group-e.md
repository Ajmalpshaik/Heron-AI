# NEEDS-CHECKING archive — Group E

> **Checks that were done, moved out of the live register.** These are rows of Group E of
> [`NEEDS-CHECKING.md`](../NEEDS-CHECKING.md) — *the refusals* — whose ID was struck through, the register's own
> sign that the check was done. Each still has its line in the register, with a link here. **Its words
> are unchanged; only its links were re-pointed.** Nothing new is written here: a new check goes in the
> register. Written by [`tools/archive-needs-checking.py`](../../tools/archive-needs-checking.py).

---
### Row E11

*Moved from the register on 2026-09-23.*

**Do this.** With **Project1 unsaved and never saved**, run a read tool (`revit_select_by_category`), then `revit_change` on that same model

**Pass looks like.** It **runs**. Until 2026-09-15 it refused every write with *"This chat has been working on Project1, but Project1 is in front in Revit now"* — the same name on both sides of the *but*, because the pin held `project:<uid>` from the read tool and read `title:Project1` back off the fragment reply, which was the one operation sending no key. `revit_use_this_model` could not clear it: it repins from `count_elements`, which sends the key again. **An unsaved model is the exposed case** — with no `documentPath` there was nothing to stand in for the missing key. Both halves are fixed (`RevitFragment.Report` now sends `documentPath` and `projectKey`; `DocumentPin.check` compares the strongest field BOTH replies carry) and **neither half has met a Revit**. `tests/test_document_pin.py` reproduces the refusal against the old code and is a text check, not a proof **RUN 2026-09-15 - PASSED, by the same run as E15.** Project1 and Project2 were both blank and never saved, so `revit_select_by_category` then `revit_change` in Project2 IS this row's arrangement: `Selected 2 ducts in Project2` then `CREATE_LEVEL ran in Project2.` One run, two rows, said so rather than recorded twice

---

### Row E12

*Moved from the register on 2026-09-23.*

**Do this.** ~~Run `revit_change` in Project1, click into a **second open project**, ask for the same change again~~ **RUN 2026-09-15 - FAILED.** Pinned to Project1, switched to Project2, asked again: **`CREATE_LEVEL ran in Project2.`** It wrote a level into the model it was NOT pointed at. **Cause: `ProjectKey` is not unique.** It is `doc.ProjectInformation.UniqueId`, which comes from the TEMPLATE - Project1, Project2 and an unrelated `PIPE.rvt` open in Revit 2020 all report `8764c510-57b7-44c3-bddf-266d86c26380-0000c160`. `here != expectProject` is therefore false between any two projects from one template, which is most projects, and the guard passes the exact case it exists to stop. See the finding below

---

### Row E13

*Moved from the register on 2026-09-23.*

**Do this.** ~~The same, but click into a **family editor** rather than a second project~~ **RUN 2026-09-15 - PASSED.** Verbatim: *"This chat has been working on a project, and \"M_Rectangular Elbow - Radius.rfa\" has no Project Information - a family, or something Heron cannot identify as the model this chat was pointed at. NOTHING was written."* The family path works because a family key is `None`, which IS distinguishable - unlike two projects, which are not

---

### Row E14

*Moved from the register on 2026-09-23.*

**Do this.** ~~In a **fresh chat**, make `revit_change` the **first** thing asked~~ **RUN 2026-09-15 - PASSED.** Pin before: `title=None project_key=None`. Result: `CREATE_LEVEL ran in Project1.` Empty really does mean *do not check*, and no chat is bricked

---

### Row E15

*Moved from the register on 2026-09-23.*

**Do this.** ~~`revit_select_by_category`, then `revit_change`, **same model, no switching**~~ **RUN 2026-09-15 - PASSED, and it proves less than it looks.** `Selected 2 ducts in Project2` then `CREATE_LEVEL ran in Project2.` Both tools DO now produce the same key - but every project on this machine produces the same key, so this row cannot tell a working identity from a colliding one. It is only meaningful once E11 is fixed

---

### Row E1

*Moved from the register on 2026-11-17.*

**Do this.** ~~Preview, wait **over 2 minutes**, then approve~~

**Pass looks like.** Refuses as **expired**. Nothing moves - **PASSED 2026-10-17 on the owner's laptop** ([the run](../needs-checking/group-e.md#the-2026-10-17-run-of-e1-e10-g1-g2-and-g4)) - *Heron D3 move test*, Revit 2024. Preview at 15:31:31, approved at 15:33:47 after a 135 s wait: *"That preview is more than 2 minutes old, so it no longer describes the model. Ask again for a fresh one - nothing was changed."* Re-read at 15:33:52: the three free ducts' lowest and highest points the same to 13 decimals (10.4106684280786 and 11.7230043860838 ft), the 725 pinned ducts' box the same

---

### Row E6

*Moved from the register on 2026-11-17.*

**Do this.** ~~Approve **twice** in a row~~

**Pass looks like.** The second finds nothing to approve. It must **not** move them a further 200 mm - **PASSED 2026-10-17, both halves** ([the run](../needs-checking/group-e.md#the-2026-10-17-run-of-e1-e10-g1-g2-and-g4)) - *Heron D3 move test*. In the chat: *"Moved 3 ducts up 200 mm in Heron D3 move test. 725 were skipped - 725 pinned."*, then at once *"There is nothing waiting to be approved. Ask for the change and Heron will show what it would do first. Nothing has been sent to Revit."* Re-read: the three ducts up 200.000 mm once (lowest point 9.75450044907594 to 10.4106684280786 ft), not 400. In the add-in: after a move had run, the SAME approval sent again from the client library came back `no_preview`, *"There is nothing waiting to be approved..."*, and the audit holds one move for it

---

### Row E7

*Moved from the register on 2026-11-17.*

**Do this.** ~~Pin a duct, then preview~~

**Pass looks like.** Reported as **skipped**, and left alone after the move - **PASSED 2026-10-17** ([the run](../needs-checking/group-e.md#the-2026-10-17-run-of-e1-e10-g1-g2-and-g4)) - *Heron D3 move test*, 725 of its 728 ducts pinned by the owner by hand for D3. Preview *"725 would be skipped - 725 pinned"*; after the move *"725 were skipped - 725 pinned"*; the audit's `skipReasons` *"725 pinned"* and `skippedElements` 725 UniqueIds. Re-read after every move that day: the 725 pinned ducts' box unchanged, 62043 x 52871 x 12615 mm, lowest point 8.13376554093945 ft, highest 49.52156081653 ft

---

### Row E19

*Moved from the register on 2026-11-17.*

**Do this.** ~~A write fragment arranged so Revit raises a **WARNING** at commit, run **with** apply - for example two elements of one category given the same Mark. **Confirm by hand first** that Revit shows it as a warning, not an error~~

**Pass looks like.** **No dialog.** The change is kept; the verdict ends *"Revit raised N warning(s) while this ran, and Heron dismissed them..."* quoting Revit's words; the reply's `warnings` is N and `warningsDismissed` lists them; **one** Ctrl+Z undoes the whole job. A dialog appearing is a FAIL - it means Revit's handling was reached - **PASSED 2026-09-23 on the owner's PC** - Revit 2024 (session 3544), model `Heron-C3-test`, a new project from his template saved in `D:\Ajmal\Heron-Proving`; add-in built from main `173a4524` and deployed to 2020, 2024 and 2027, the three installed. **By hand first:** the owner gave both walls the Mark `TEST` and Revit kept it (re-read, 2 of 2) - his words: *"revit will give the wanring that is duplicat vale but we can ingore that"*. Then `heron_bridge_client.py fragment write-element-parameters --write --apply --set parameterName=Mark --set value=HERON-E19` on the two walls: *"APPLIED 'write-element-parameters' was KEPT ... Revit raised 1 warning while this ran, and Heron dismissed it rather than let a dialog stop the job: "Elements have duplicate "Mark" values." (2 elements). Dismissing a warning removes the message, not its cause"* - the singular is the code's form for N = 1; `warnings` 1, `warningsDismissed` one entry, that text, times 1, elements 2. Re-read: 2 of 2 walls carry HERON-E19, and Revit's own warning list still holds the warning (`read-model-warnings`, 1). No dialog (owner). **One Ctrl+Z:** both Marks empty, 0 of 2 HERON-E19 and 0 of 2 `TEST` - only Heron's job came back - 4 grids, 3,416 elements as before

---

### Row E20

*Moved from the register on 2026-11-17.*

**Do this.** ~~E19 **without** apply~~

**Pass looks like.** *"NOTHING WAS KEPT"*, the same warning sentence, no dialog - and the value E19 wrote is **not** in the model when re-read - **PASSED 2026-09-23 on the owner's PC** - the same walls, `fragment write-element-parameters --write --set parameterName=Mark --set value=HERON-E19`, no apply: *"ROLLED BACK  NOTHING WAS KEPT. 'write-element-parameters' ran for real and Revit reported the transaction group rolled back afterwards ... Revit raised 1 warning while this ran, and Heron dismissed it rather than let a dialog stop the job: "Elements have duplicate "Mark" values." (2 elements)..."*; `applied` false, `rolledBack` true, `warnings` 1. Re-read: 0 of 2 walls carry HERON-E19, both Marks empty, no warning stored, 3,416 elements. No dialog (owner). The rollback cleared Revit's selection, as the proving notes have long said

---

### Row E21

*Moved from the register on 2026-11-17.*

**Do this.** ~~A write fragment arranged so Revit raises an **ERROR** at commit. Candidate: lock a dimension between two grids, then move one grid - check by hand first that Revit calls it an error and offers a resolution (a *Remove Constraints* style button), which is exactly the resolution this must never apply~~

**Pass looks like.** `operation_failed`, quoting Revit's error in its own words, and *"Revit confirmed the rollback"*. **Nothing deleted and nothing unlocked:** the element count, the grids' positions and the lock are all as before. No dialog. **A missing lock or a missing element is the failure this row exists for** - **PASSED 2026-09-23 on the owner's PC** - grids A and B 6000 apart (`CREATE_GRIDS`), grid B pinned (`SET_PIN_STATE`), and the owner drew the A-B dimension and locked it. **By hand first**, moving grid A: Revit's own box, *"Error - cannot be ignored / Constraints are not satisfied"*, with **Remove Constraints**, OK greyed and Cancel - he pressed Cancel (his screenshot). Then `fragment move-elements --write --apply --set offset=500,0,0` on grid A: `operation_failed` - *"'move-elements' ran, and Revit would not keep it. Revit would not let it through: "Constraints are not satisfied" (3 elements). Heron rolled the whole job back rather than let Revit resolve it its own way - Revit's own fix for an error can be to delete the elements it names. Revit confirmed the rollback, so the model is exactly as it was."* Re-read: 3,418 elements as before, the dimension still there, `report-constraints` still finds the one constraint holding A and B (grids 1 and 2 free), grid B still pinned. No dialog, and the dimension still 6000 and locked (owner, screenshot). **The positions were checked by eye, not read:** no proven read reports where a grid is - `report-location` finds no point and no box on one

---

### Row E22

*Moved from the register on 2026-11-17.*

**Do this.** ~~E21's write as a **setup step** in front of any fragment~~

**Pass looks like.** `setup_failed`, naming the step and saying the fragment under test never ran; the model as before, re-read - **PASSED 2026-09-23 on the owner's PC** - grid A selected, then `heron_bridge_client.py validate --out <scratch file> list-grids --write --setup move-elements --set offset=500,0,0`. `move-elements` is MODIFY, so the client sends it in the request's `setup` array and the add-in runs it inside the fragment's own group: `setup_failed` - *"Revit would not keep the arrangement step 'move-elements', so the fragment under test never ran. Revit would not let it through: "Constraints are not satisfied" (3 elements, in setup step 'move-elements'). Heron rolled the whole job back rather than let Revit resolve it its own way ... Revit confirmed the rollback, so the model is exactly as it was."* Re-read as E21: 3,418 elements, the dimension, the constraint and the pin all as before. No dialog, the dimension 6000 and locked (owner)

---
