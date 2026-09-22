# NEEDS-CHECKING archive — Group Z

> **Checks that were done, moved out of the live register.** These are rows of Group Z of
> [`NEEDS-CHECKING.md`](../NEEDS-CHECKING.md) — *the renamed ribbon and the Bridge Status window, 2026-09-20* — whose ID was struck through, the register's own
> sign that the check was done. Each still has its line in the register, with a link here. **Its words
> are unchanged; only its links were re-pointed.** Nothing new is written here: a new check goes in the
> register. Written by [`tools/archive-needs-checking.py`](../../tools/archive-needs-checking.py).

---
### Row Z1

*Moved from the register on 2026-09-23.*

**Do this.** ~~Deploy, start Revit, look at the ribbon~~

**Pass looks like.** A tab reading **Heron**, holding a panel reading **AI Bridge**. Not `Heron AI`, and not a second tab beside it - if both appear, `CreateRibbonTab` made a new one instead of finding the old, and the old add-in is still deployed. **PASS 2026-09-20, Revit 2024.3, `test projject.rvt`**, from a screenshot the owner took after restarting rather than from anything Heron reported about itself. **ONE** tab reading **Heron**, sitting between `DiRootsOne` and `Modify`, holding **one** panel labelled **AI Bridge**. No `Heron AI` tab beside it, so `CreateRibbonTab` found nothing to collide with and no stale deploy is left over. The split button reads `Heron` and the padlock reads `Changes ON`, both drawn with their icons - which also re-proves `A12`'s ribbon question on the renamed tab. **Not proved by this**: anything behind the split button's arrow, which is where `Bridge Status` lives - that is `Z2`

---

### Row Z2

*Moved from the register on 2026-09-23.*

**Do this.** Press **Bridge Status** with the bridge connected

**Pass looks like.** The window, not a TaskDialog. Blue strip, blue lamp with a glow, `Connected`, and the pipe name in `Consolas`. The `Close` button is the blue one. **PROVED 2026-09-20 from the add-in's own log, not from a description.** Revit 2024 `pid 15920` started `19:38:25Z`, thirteen minutes AFTER that release's assembly was installed at `19:24:50Z`, so it is the new build. At `19:39:39Z` the log reads **`Connected from Bridge Status`** - a string written by `HeronBridgeToggle` and reachable ONLY from a button inside the drawn window. Had the window not drawn, `Show` returns false and `ShowPlainStatus` runs, which has no toggle in it at all. **Zero** lines matching `failed to open` anywhere in the day's log

---

### Row Z3

*Moved from the register on 2026-09-23.*

**Do this.** Press it with the bridge NOT connected

**Pass looks like.** Grey strip, lamp with **no** glow, `Not connected`, no `Pipe` row and no `ANNOUNCED IN` section. The blue button reads **Connect this session**. **PROVED 2026-09-20, same line.** The preceding entry at `19:39:27Z` is `Disconnected from the ribbon`, so the window opened onto the NOT-connected state, and the button pressed twelve seconds later was the primary one - which in that state is `Connect this session` and nothing else

---

### Row Z9

*Moved from the register on 2026-09-23.*

**Do this.** Do Z1 and Z2 on **2020** as well as on a modern release

**Pass looks like.** 2020 is `net472` and 2027 is `net10.0-windows` - two different WPF stacks under the same source. `ControlTemplate` built from `FrameworkElementFactory` is the part most likely to differ, and the buttons are where it would show. **MOSTLY PROVED 2026-09-20, on Revit 2020.2.9.** `pid 26308` loaded at `19:42:15Z`, eighteen minutes after that release's assembly was installed at `19:24:44Z`, so it is the net472 build. **The ribbon was seen directly**, in a screenshot of Revit 2020 itself: one **Heron** tab, one **AI Bridge** panel, the **Heron** split button and the **Changes ON** padlock, both drawn with their icons. **The Changes window drew on net472 too**, by the same absent-fallback argument as `Z10`: `Write permission set to False` at `19:43:00Z` and `True` at `19:43:04Z`, the ON direction being the one that asks, with no `failed to open` line anywhere in the day. **`19:42:57Z Connected from the ribbon`** says the bridge button works there as well. **AND BRIDGE STATUS TOO, 2026-09-20**, from a screenshot of the window itself on Revit 2020. It identifies its own session and the identifiers match the log exactly: pipe `heron.2020.26308`, process `26308`, Revit `2020`, add-in `0.1.0.0`, protocol `2`, announced in `...bridges\26308.json`. Blue lamp on `Connected`, the `Changes` card amber with an `ON` chip - which agrees with `Write permission set to True` at `19:43:04Z` - and the discovery path wrapping onto two lines as `PathLine` is built to. So `FrameworkElementFactory` templates, the inset boxes, the wrap and the chip all draw on net472. **Z9 is answered on every part except a 150% display**, which is `Z8` and is nobody's to answer but a monitor's

---

### Row Z10

*Moved from the register on 2026-09-23.*

**Do this.** Press the **Changes** padlock while it is OFF

**Pass looks like.** The Changes window, not a TaskDialog. Amber strip, amber lamp, `Let Heron change this model?`, three green ticks. The amber button reads **Turn changes on**. **PROVED 2026-09-20, twice, and the second time is the better one.** First by what was ABSENT: on `pid 15920` write went `False` then `True`, only the ON direction asks, and the TaskDialog fallback runs only on an exception that is itself logged - of which there were none. Sound, and still an argument. **Then as a fact**: after the windows were taught to say so, `pid 19600` logged **`Changes window asked.`** at `20:38:00Z`, one second before the setting moved. No inference left in it

---

### Row Z13

*Moved from the register on 2026-09-23.*

**Do this.** Press the padlock again, while it is ON

---
