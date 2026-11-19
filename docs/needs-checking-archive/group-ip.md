# NEEDS-CHECKING archive — Group IP

> **Checks that were done, moved out of the live register.** These are rows of Group IP of
> [`NEEDS-CHECKING.md`](../NEEDS-CHECKING.md) — *a form pictured by the id the form tools give it: `revit_view_image` with `elements` takes a UniqueId (2026-10-22)* — whose ID was struck through, the register's own
> sign that the check was done. Each still has its line in the register, with a link here. **Its words
> are unchanged; only its links were re-pointed.** Nothing new is written here: a new check goes in the
> register. Written by [`tools/archive-needs-checking.py`](../../tools/archive-needs-checking.py).

---
### Row IP1

*Moved from the register on 2026-11-17.*

**Run.** *Negative, in a project.* *Heron Capture Test* in front; `elements` = the box's UniqueId from IP0's family

**Look for.** `no_such_elements` naming it, *"An id belongs to one model, or one family"*. Nothing pictured. The D-30 negative for the project half **Struck 2026-11-13:** PASSED - the verdict is in this file's *Results - 2026-10-22, the owner's laptop: Revit 2020.2.1 (20.2.1.1), session 60544*, which records it run and passed.

---

### Row IP2

*Moved from the register on 2026-11-17.*

**Run.** *The positive, in the family.* Back in the family; `elements` = the box's UniqueId alone

**Look for.** **A picture of the box alone**, close up, in the chat; the words say 1 element framed. The D-30 positive **Struck 2026-11-13:** PASSED - the verdict is in this file's *Results - 2026-10-22, the owner's laptop: Revit 2020.2.1 (20.2.1.1), session 60544*, which records it run and passed.

---

### Row IP3

*Moved from the register on 2026-11-17.*

**Run.** *Both forms by UniqueId*

**Look for.** Both in the picture, 2 framed - the cylinder, at the family's own subcategory, framed too (row 5b-1311). *"has no category"* for it means the build is missing that fix **Struck 2026-11-13:** PASSED - the verdict is in this file's *Results - 2026-10-22, the owner's laptop: Revit 2020.2.1 (20.2.1.1), session 60544*, which records it run and passed.

---

### Row IP4

*Moved from the register on 2026-11-17.*

**Run.** *Mixed, in the project.* `elements` = duct 1's number, duct 2's UniqueId; then duct 1's number and duct 1's UniqueId

**Look for.** The first: 2 ducts framed. The second: **1 framed** - one duct named two ways is pictured once **Struck 2026-11-13:** PASSED - the verdict is in this file's *Results - 2026-10-22, the owner's laptop: Revit 2020.2.1 (20.2.1.1), session 60544*, which records it run and passed.

---

### Row IP5

*Moved from the register on 2026-11-17.*

**Run.** *Nothing kept.* Count the family's forms (`REPORT_FAMILY_FORMS` "all") and the project's elements before IP2 and IP4, and after; open Undo's list

**Look for.** The same counts; no Heron entry in Undo; the words say the temporary view was rolled back **Struck 2026-11-13:** PASSED - the verdict is in this file's *Results - 2026-10-22, the owner's laptop: Revit 2020.2.1 (20.2.1.1), session 60544*, which records it run and passed.

---
