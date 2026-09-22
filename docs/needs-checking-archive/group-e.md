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
