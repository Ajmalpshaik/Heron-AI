# NEEDS-CHECKING archive — Group J

> **Checks that were done, moved out of the live register.** These are rows of Group J of
> [`NEEDS-CHECKING.md`](../NEEDS-CHECKING.md) — *the executor's inputs (needs Revit, and something selected)* — whose ID was struck through, the register's own
> sign that the check was done. Each still has its line in the register, with a link here. **Its words
> are unchanged; only its links were re-pointed.** Nothing new is written here: a new check goes in the
> register. Written by [`tools/archive-needs-checking.py`](../../tools/archive-needs-checking.py).

---
### Row J1

*Moved from the register on 2026-09-23.*

**Do this.** ~~Select a few ducts, then `prove count-elements`~~

**Pass looks like.** **DONE 2026-09-07.** Five ducts selected; `<- elements from the selection (5)`, `count 5`, `countedNothing false`. The number matched the selection and the answer said where it came from. **The first fragment needing an input ever to run in this project**

---

### Row J2

*Moved from the register on 2026-09-23.*

**Do this.** ~~Select nothing, then run J1 again~~

**Pass looks like.** **DONE 2026-09-07 — the row that mattered most, and it held.** `needs_unbound`: *"'elements (IList&lt;Element&gt;)' was never supplied. Nothing is selected in Revit, and no earlier fragment in this session left a value of that name. Running anyway would report 0 results, which reads as 'there was nothing to find' rather than 'nobody was asked'."* **Exit code 1**, so a script cannot read it as success either. It did NOT report `count 0`

---

### Row J3

*Moved from the register on 2026-09-23.*

**Do this.** ~~`prove <a filter> count-elements`~~

**Pass looks like.** **DONE 2026-09-07, with `list-levels` rather than `filter-elements-by-category`** — the latter needs caller values and is `J5`. `list-levels` found 2 levels; `count-elements` then read `<- elements from list-levels (2)`, `count 2`. **Five ducts were selected at the time and it did not use them**, which is the part worth having: the chain takes precedence over the selection and says which it used. [D-29](../DECISIONS.md)'s filter-feeding-action, running for the first time

---

### Row J4

*Moved from the register on 2026-09-23.*

**Do this.** ~~`prove unjoin-geometry` with a selection~~

**Pass looks like.** **DONE 2026-09-07.** `needs_unbound` naming both `first (IList<Element>)` and `second (IList<Element>)`: *"There is a selection, but this fragment needs 2 separate sets of elements and one selection cannot say which is which."* It counted the ambiguity rather than picking one

---

### Row J5

*Moved from the register on 2026-09-23.*

**Do this.** ~~`prove` a fragment needing a category or a name~~

**Pass looks like.** **DONE 2026-09-07.** `filter-elements-by-category` refused with `needs_request_values`, naming **both** `category (BuiltInCategory)` and `levelId (ElementId)` with their types. **278 fragments are behind this**, and it is the next unlock — bigger than this one was

---

### Row J6

*Moved from the register on 2026-09-23.*

**Do this.** ~~Run a batch, then run another, and check it does not inherit~~

**Pass looks like.** **DONE 2026-09-07, and run the sharper way round.** After the `J3` batch left 2 levels carried, a FRESH `prove count-elements` returned `<- elements from the selection (5)` — the five ducts, **not** the two carried levels. Had `chain: reset` not fired it would have said `from list-levels (2)`, so the two outcomes are distinguishable rather than both plausible

---
