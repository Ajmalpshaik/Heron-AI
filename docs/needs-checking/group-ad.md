# Needs checking — Group AD

> One group of [the register](../NEEDS-CHECKING.md), in its own file since 2026-09-23 so that it can be read
> alone. **The register's rules, and every group's place in it, are on that page.** A new row
> for this group goes in this file. [`tools/needs-checking-register.py`](../../tools/needs-checking-register.py)
> reads it back into the register for every tool that reads the register, so a row here is seen
> exactly as it was seen there. Written by
> [`tools/split-needs-checking.py`](../../tools/split-needs-checking.py).

## 2026-09-22 — STAGE 7: UNINSTALL IS THE SAME WINDOW, and the dangerous part was not the deleting

`tools/deploy-addin.ps1 -Remove` already worked and was proven by `AA4`. What this adds is the window
driving it — and **the safety property without which `R-21` is a disaster.**

### The thing that would have gone wrong

**Every product row used to start EMPTY.** `R-21` says unticking a product uninstalls it. Put those two
together and a user who opened the window and pressed Install **without touching anything** would have
unticked everything they had — and the press meant to install would have **removed the lot**.

**So what is installed now starts ticked.** Unticking is then something a person did on purpose, which
is the only ground on which a delete may be offered at all. It is checked, and it was **seen to fail**:
forcing rows back to unticked turns that check red.

### And it confirms, naming what goes

> *This will REMOVE 'AI Bridge connector' from Revit 2020, 2024 and 2027.*
> *Restart Revit afterwards to unload it.*
> *Your own Heron data is not touched — settings, the audit log and anything Heron has learned all stay
> where they are.*

**A safety gate, not an information message** — the house rule is that anything which deletes confirms
first and says what will happen and to how much. It **defaults to No**: a delete must not be one stray
Enter away. Saying no changes **nothing at all** — not the ticks, not the install, not one file.

**And there is no dialog when nothing is being removed.** A confirmation people meet every time is one
they stop reading.

### What was actually run, and what it said

| | |
|---|---|
| **PASS** | The uninstall decisions against a fake Revit and a fake disk: installed starts ticked; unticking removes from **every release it is on**, not the ones merely highlighted at the top of the window; a heading removes nothing of its own (D-93); removals run **before** installs; an uninstall **waits for Revit** exactly as an install does; one removal failing does not stop the rest |
| **PASS** | `tests/test_installer_window.py` — confirms **before** handing the work off, defaults to No, and there are still exactly **two** buttons |
| **PASS** | The ten gates, and all 13 projects on all 8 releases |
| **NOT RUN** | **Nothing has been deleted from a real Addins folder.** `DeployScriptDeployer.Remove` has never executed a line, and no window has been drawn |
| **NOT MET** | The audit line — see below |
| **NEEDS REAL REVIT** | `AD1` to `AD4` below |

**Seen to fail:**

| what was broken | red |
|---|---|
| rows forced back to starting unticked | **1** — and it is the one that matters most |
| a heading allowed into the removal list | **6** |
| installs moved ahead of removals | **2** |
| the confirmation removed | **3** |
| the "your data is not touched" sentence dropped | **1** |

**The ordering break took two attempts, and the first was a false green.** Swapping the two loops by
searching for the first `foreach` found the pair in the *touched releases* block above, not the real
ones, so the surgery was a no-op and the check stayed green. **A break that does not break is a check
not tested** — it was redone against the loops after the wait, and then it went red.

### One requirement CANNOT be met the way it is written

`R-27` says every install, uninstall and update writes an audit line **through `HeronAudit`**. The
installer **may not reference `Heron.Core`** — its own `.csproj` says so: the engine is
release-independent and `Heron.Core` is not, and Stage 4 already tried and undid exactly that reference
for `HeronPaths`.

**So R-27 is written against a route that does not exist**, and nothing noticed because nothing had
tried to write an audit line from the installer before. Four ways out are in
[Q-PE-15](../work-notes/plans/plugin-extension/03-open-questions.md). It is a **SHOULD**, so Stage 7 was
built without it rather than stopping — but **an uninstall that leaves no trace is the operation you
most want a trace of.**

### Rollback is deliberately not in the window

`R-23a` allows no third button, and `-Rollback` is one command a person runs on purpose after an update
has gone wrong. It was **proven on a real machine by `AA8`** on 2026-09-21, which is what Stage 7 item 4
asks for.

### Rows for Ajmal's PC — four

**Do these on a Revit you can afford to break.** Every other group has been safe to run; this one
deletes.

| ID | Do this | Pass looks like |
|---|---|---|
| **AD1** | Install the AI Bridge, close the window, reopen it | Its row is **already ticked**, and reads `Installed`. **This is the row that matters most** — if it opens unticked, pressing Install would remove it, and the next row is dangerous to run |
| **AD2** | Untick it and press Install | **A dialog appears first**, naming the product and every Revit release it is on, saying your data is untouched and to restart Revit. Press **No** — nothing changes, the window says *"Nothing was changed."*, and the folder is still there. Then do it again and press **Yes** |
| **AD3** | After `AD2`'s Yes, look at the disk | `%APPDATA%\Autodesk\Revit\<ver>\` no longer holds the product's folder or its `.addin`. **And `%APPDATA%\Heron` is untouched** — settings, audit log, everything Heron has learned. Check that folder by hand; it is `R-22` and it is the promise that matters |
| **AD4** | **Leave Revit open**, untick a product, press Install, confirm | It **waits** and says which Revit is open, exactly as an install does. Close Revit without touching the window — the removal then carries on by itself. **Nothing must have been deleted before Revit was closed** |

**`AD1` is the gate on the other three.** If an installed product opens unticked, do not run `AD2` —
report it instead, because at that point the window is one press away from removing things nobody
unticked.

---
