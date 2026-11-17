# NEEDS-CHECKING archive — Group D

> **Checks that were done, moved out of the live register.** These are rows of Group D of
> [`NEEDS-CHECKING.md`](../NEEDS-CHECKING.md) — *the move itself* — whose ID was struck through, the register's own
> sign that the check was done. Each still has its line in the register, with a link here. **Its words
> are unchanged; only its links were re-pointed.** Nothing new is written here: a new check goes in the
> register. Written by [`tools/archive-needs-checking.py`](../../tools/archive-needs-checking.py).

---
### Row D1

*Moved from the register on 2026-11-17.*

**Do this.** ~~*"move the ducts up 200 mm"*~~

**Pass looks like.** **PASSED 2026-10-17.** The preview read *"This would move 3 ducts up 200 mm in Heron D3 move test. 725 would be skipped - 725 pinned."* - the 3 free ducts the arrangement left, and every pinned one named as skipped. Read straight after it: all four test ducts still at **3079 mm**. Nothing moved

---

### Row D2

*Moved from the register on 2026-11-17.*

**Do this.** ~~Say yes~~

**Pass looks like.** **PASSED 2026-10-17.** Ajmal PS said yes in the chat; `revit_apply_move` answered *"Moved 3 ducts up 200 mm in Heron D3 move test. 725 were skipped - 725 pinned."*

---

### Row D3

*Moved from the register on 2026-11-17.*

**Do this.** ~~**MEASURE ONE.**~~

**Pass looks like.** **PASSED 2026-10-17 - all three measured, and each is +200.0000 mm.** End-point Z **3173.1717 -> 3373.1717 mm** and Middle Elevation **3079 -> 3279 mm** on ducts 892432, 892448 and 892476; X, Y and length unchanged to 0.0001 mm. A second route agrees: the PROVEN `REPORT_BOUNDING_BOX` over the same three read `minZ` **9.75450044907594 -> 10.4106684280786 ft**, which is 0.65616798 ft = **200.0000 mm**. Not 200 feet, not 0.656 of anything. **The negative held:** the pinned control duct 892462, drawn beside them, and an original pinned duct 392168 did not move at all

---

### Row D4

*Moved from the register on 2026-11-17.*

**Do this.** ~~Look at the screen without clicking or zooming~~

**Pass looks like.** **PASSED 2026-10-17, measured on the window, not judged by eye.** Revit's own window was captured (PrintWindow, nothing else on the screens) in `{3D}` before the move and straight after it, nobody clicking between. The three ducts moved **24 px further up** the picture than two air terminals that were not moved. The whole picture had also shifted 51 px between those two frames - the first was taken a second after Heron zoomed to the ducts, so the camera was still settling; not measured further. After the undo the check is clean: the air terminals moved **0 px** and the ducts came down **24-28 px**

---

### Row D5

*Moved from the register on 2026-11-17.*

**Do this.** ~~**One** Ctrl+Z~~

**Pass looks like.** **PASSED 2026-10-17.** Ajmal PS opened Revit's Undo list: **one** entry, *"Heron: move ducts up 200 mm"*, above his own *"Pin"* and *"Duct"* steps. **One** Ctrl+Z, and all three read **3079 mm** again with the 725 pins still in place. He also pressed Redo by accident: back to 3279, and a second single Ctrl+Z put them back again

---

### Row D6

*Moved from the register on 2026-11-17.*

**Do this.** ~~*"move them down 50 mm"*~~

**Pass looks like.** **PASSED 2026-10-17.** The preview read *"3 ducts down 50 mm"*; after his yes, all three went **3079 -> 3029 mm (-50.0000 mm)**, the pinned control stayed at 3079, and the undo entry was named *"Heron: move ducts down 50 mm"* - a direction, not "up -50". One Ctrl+Z put them back to 3079

---
