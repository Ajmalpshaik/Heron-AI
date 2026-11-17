# NEEDS-CHECKING archive — Group G

> **Checks that were done, moved out of the live register.** These are rows of Group G of
> [`NEEDS-CHECKING.md`](../NEEDS-CHECKING.md) — *hard to force, do last* — whose ID was struck through, the register's own
> sign that the check was done. Each still has its line in the register, with a link here. **Its words
> are unchanged; only its links were re-pointed.** Nothing new is written here: a new check goes in the
> register. Written by [`tools/archive-needs-checking.py`](../../tools/archive-needs-checking.py).

---
### Row G4

*Moved from the register on 2026-11-17.*

**Do this.** ~~After a move, open the newest file in `%APPDATA%\Heron\audit`~~

**Pass looks like.** The entry carries **every moved element's UniqueId**, the document identity and the undo entry name, all under one Workflow ID. A count alone cannot answer *"which ducts?"* - **PASSED 2026-10-17 on the owner's laptop** ([the run](../needs-checking/group-e.md#the-2026-10-17-run-of-e1-e10-g1-g2-and-g4)) - `audit-202610.jsonl`, the move at 15:30:35 on *Heron D3 move test*: one entry, `wf-20261017-123035-0060-4e4f`, carries `elements` = the three moved ducts' UniqueIds (ending `-000d9e10`, `-000d9e20`, `-000d9e3c`: 892432, 892448 and 892476, the three free ducts), `documentId` `8764c510-...-0000c160`, `undoEntry` *Heron: move ducts up 200 mm*, `moved` and `attempted` 3, and the 725 skipped ducts' UniqueIds. **The dispatcher's own line for the same request carries another id, `wf-20261017-123035-0059-ca43`** - [row 5b-1061](../fragment-issues/section-5b-rows-1051-1075.md)

---
