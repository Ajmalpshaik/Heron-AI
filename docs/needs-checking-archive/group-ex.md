# NEEDS-CHECKING archive — Group EX

> **Checks that were done, moved out of the live register.** These are rows of Group EX of
> [`NEEDS-CHECKING.md`](../NEEDS-CHECKING.md) — *the run guard: a running job cannot save, sync, export or print what its operation does not allow (2026-10-17)* — whose ID was struck through, the register's own
> sign that the check was done. Each still has its line in the register, with a link here. **Its words
> are unchanged; only its links were re-pointed.** Nothing new is written here: a new check goes in the
> register. Written by [`tools/archive-needs-checking.py`](../../tools/archive-needs-checking.py).

---
### Row EX1

*Moved from the register on 2026-11-17.*

**Run.** Changes and Publish on. *Heron Guard A* in front, *Heron Guard B* open behind it with one unsaved change. `python mcp/client/heron_bridge_client.py fragment --in "Heron Guard A" save-document --write --apply --set "documentPath=<Heron Guard B.rvt>" --set closeWindow=false`

**Look for.** `saved true`; B's file time moves; no `run_guard`; the audit line carries no `blocked`. **save-document still works under Publish** - the guard stood aside. Then the same with Publish OFF: `publish_disabled`, nothing sent - the gate's, unchanged **Struck 2026-11-13:** PASSED - the verdict is in this file's *The 2026-10-18 run*, which records it run and passed.

---

### Row EX9

*Moved from the register on 2026-11-17.*

**Run.** **A change to another open model.** A scratch MODIFY probe that opens a `Transaction` in the open *Heron Guard B*, draws a model line there and commits it, run on A with `--write --apply`

**Look for.** `changed_other_model`: *"It changed 'Heron Guard B' ... THAT CHANGE IS COMMITTED ... HERON CANNOT UNDO IT"*, A rolled back and confirmed; the line IS in B and B's own Undo lists it. **The negative**: the same probe aimed at A's own `doc` is kept, and its audit line carries no `blocked`. And a copy whose card says `risk: PUBLISH`, Publish on - the guard stands aside and nothing is refused **Struck 2026-11-13:** PASSED - the verdict is in this file's *The 2026-10-18 run*, which records it run and passed.

---

### Row EX10

*Moved from the register on 2026-11-17.*

**Run.** **A new document under a change.** A scratch MODIFY probe that calls `app.NewFamilyDocument` on the Generic Model template and then `SaveAs` to a scratch path

**Look for.** `run_guard` naming the new document and the Admin switch; the scratch path has NO file (its `SaveAs` was cancelled); the log's `Run guard closed` line names the new document. **The negative** is EX6's `create-family-document` under Admin **Struck 2026-11-13:** PASSED - the verdict is in this file's *The 2026-10-18 run*, which records it run and passed.

---
