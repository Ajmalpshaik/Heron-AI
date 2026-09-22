# NEEDS-CHECKING archive — Group U

> **Checks that were done, moved out of the live register.** These are rows of Group U of
> [`NEEDS-CHECKING.md`](../NEEDS-CHECKING.md) — *a review found six things in Group T's own work, and all six held* — whose ID was struck through, the register's own
> sign that the check was done. Each still has its line in the register, with a link here. **Its words
> are unchanged; only its links were re-pointed.** Nothing new is written here: a new check goes in the
> register. Written by [`tools/archive-needs-checking.py`](../../tools/archive-needs-checking.py).

---
### Row U1

*Moved from the register on 2026-09-23.*

**What it found.** `elements` sat in the unconditional envelope skip, so a **list** by that name was never traversed - and `read_parameters` and `list_groups` both return their per-element answer under exactly that name. Two models with equal totals and different contents compared as *nothing moved*. A skip inherited from the number-only behaviour, silently cancelling the list traversal added the same day

**State.** **FIXED.** `SCALAR_ONLY_SKIP` - skipped where it is the scalar total, compared where it is a list. Both cases tested, and the same-reply-twice guard re-checked

---

### Row U2

*Moved from the register on 2026-09-23.*

**What it found.** `track --arg` made the arguments part of the test and the draft recorded only `operation`, so `PAR-011` and `SEL-008` never said `category=Ducts` was supplied. Not reproducible, and a later run with a different category would be indistinguishable

**State.** **FIXED.** The draft carries an `arguments:` line

---

### Row U3

*Moved from the register on 2026-09-23.*

**What it found.** Four register sections still read **Never run** for agents that had been tracked and signed hours earlier

**State.** **FIXED.** `PHS-032`, `SYS-030`, `PAR-011`, `GRP-033` corrected - and corrected *without* closing their detailed cases, which tracking does not touch. `LNK-015` still reads Never run, correctly

---

### Row U4

*Moved from the register on 2026-09-23.*

**What it found.** Row 118 was claimed twice - by this branch and by [PR #191](https://github.com/Ajmalpshaik/Heron-AI/pull/191) the same day

**State.** **FIXED** in the merge. Theirs keeps 118-125, mine is **126**

---
