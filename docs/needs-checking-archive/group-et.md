# NEEDS-CHECKING archive — Group ET

> **Checks that were done, moved out of the live register.** These are rows of Group ET of
> [`NEEDS-CHECKING.md`](../NEEDS-CHECKING.md) — *Revit asks Keep or Discard before a change from a chat is kept: D-126 (2026-10-17)* — whose ID was struck through, the register's own
> sign that the check was done. Each still has its line in the register, with a link here. **Its words
> are unchanged; only its links were re-pointed.** Nothing new is written here: a new check goes in the
> register. Written by [`tools/archive-needs-checking.py`](../../tools/archive-needs-checking.py).

---
### Row ET2

*Moved from the register on 2026-11-17.*

**Run.** Same model and view. Select the two walls. Note the element count of the Levels (e.g. a level schedule or Project Browser). Ask the same level again, `ET Test 2` at 7000 mm, and choose **Discard it**

**Look for.** The level count is **unchanged**, there is **no** new `Heron:` entry at the top of the Undo list, the elevation is not isolated, and the **two walls are selected again** - the modeller's own selection, put back. The chat says *"The modeller chose Discard in Revit, so NOTHING WAS KEPT"* and does **not** try again or try another capability. **The D-30 negative** - **Struck 2026-10-18:** PASS on Revit 2024, 2026-10-18 - the results table under *The 2026-10-18 run* below.

---

### Row ET3

*Moved from the register on 2026-11-17.*

**Run.** Ask the same level twice more. The first time press **Esc**; the second time close the dialog with its **X**

**Look for.** Both times exactly ET2's result: nothing kept, no undo entry, selection back. Only the Keep link keeps - **Struck 2026-10-18:** PASS on Revit 2024, 2026-10-18 - the results table under *The 2026-10-18 run* below.

---

### Row ET5

*Moved from the register on 2026-11-17.*

**Run.** Open a second scratch project (*Heron Keep Back.rvt*), leave it in front, and ask the chat to create the level in *Heron Keep Question* by name

**Look for.** The dialog says the model **is not the one in front** and nothing is selected or isolated in *either* window. Keep works as ET1 in the named model; nothing appears in the model in front - **Struck 2026-10-18:** PASS on Revit 2024, 2026-10-18 - the results table under *The 2026-10-18 run* below.

---

### Row ET10

*Moved from the register on 2026-11-17.*

**Run.** With the switch on, ask the chat to open the South elevation (`OPEN_VIEW`) while a plan is in front

**Look for.** **No dialog** - Revit reports no change to the model - and the verdict says *"the modeller was not asked"*. The view opens - **Struck 2026-10-18:** PASS on Revit 2024, 2026-10-18 - the results table under *The 2026-10-18 run* below.

---

### Row ET12

*Moved from the register on 2026-11-17.*

**Run.** *Watching, not a proof.* On every row above that shows the question

**Look for.** It waits for a click. If it ever closes by itself, or the chat reports `dialog_refused` or a Discard nobody chose, the job's dialog watch (Group EL) is answering Heron's own question and `HeronDialogWatch.HeronAsks()` is not standing it down - write down the row and what the chat said - **Struck 2026-10-18:** PASS on Revit 2024 and 2027, 2026-10-18 - the results table under *The 2026-10-18 run* below.

---
