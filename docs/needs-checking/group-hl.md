# Needs checking — Group HL

> One group of [the register](../NEEDS-CHECKING.md), in its own file since 2026-10-18 so that it can be read
> alone. **The register's rules, and every group's place in it, are on that page.** A new row
> for this group goes in this file. [`tools/needs-checking-register.py`](../../tools/needs-checking-register.py)
> reads it back into the register for every tool that reads it, so a row here is seen
> exactly as it was seen there.

## Group HL - anything a kept change deletes is asked in Revit first: D-142 (2026-10-18)

[D-142](../decisions/D-142.md) (the owner's decision, 2026-10-18), from [row 5b-1180](../fragment-issues/section-5b-rows-401-425.md).
(HH to HK were left free when this was written: other sessions were adding groups the same day.)
**What changed.** A change that would be KEPT - `revit_change`, the words alone, the command line's
`--write --apply`, a DRAFT's `try-once` - in which Revit counts any element deleted is rolled back, and
Revit shows the elements that would go, selected and zoomed to, in a box of its own: *"Delete N
elements?"*, the chat's words, the model, the count by category with *"(goes with the ...)"*, and two links,
**Delete** and **Don't delete**. Whatever *Ask to Keep* says, and never under a job's grant. On Delete the
same change runs again and is kept only if it deletes exactly what was shown.

**What is tested and not run.** It compiles for 2020 to 2027. The box's words, its grouping and the
same-set rule run without Revit in `tests/Heron.KeepQuestion.TestHost` sections 11 to 15; the wiring, the
client's wait and the reply in `tests/test_delete_box.py`. **Nothing here has run in Revit**, and none of
it is to be run until the owner says so. Needs the add-in rebuilt from this change and deployed with Revit
closed, the chat restarted, Changes ON, and a **scratch copy** of a project with walls that host doors -
never a working model.

| # | Run | Look for |
|---|---|---|
| **HL1** | *Ask to Keep off - the default.* Select two walls with no doors in them and type *"delete these"* | **The delete box is up**, titled *"Delete 2 elements?"* (or more, with what goes along), naming the model and quoting *delete these*; the walls are selected and the view is zoomed to them; Delete and Don't delete, nothing else. Press **Delete**: both walls gone, ONE undo entry `Heron: delete-elements`, and the chat's reply begins *"The modeller was asked in Revit whether to delete ..."* and *"pressed DELETE"*. **FAIL:** the walls gone with no box |
| **HL2** | *Under the grant.* Turn Ask to Keep on, make one change and press its third link (*keep the rest of this job*, D-138); then, from the same chat, select one wall and type *"delete these"* | **The delete box is up** all the same, with no third link; after Delete the reply does NOT say *"Kept WITHOUT ASKING"*. The ribbon still reads `Keep: rest of job`, and the next change that deletes nothing is kept with no box |
| **HL3** | *A wall with doors.* Select one wall that hosts two doors and type *"delete these"* | The title counts the doors too - *"Delete 3 elements?"* or more - and the list reads *Walls 1* and *Doors 2 (goes with the Walls)*, with anything else Revit removes; the doors are selected with the wall. Press **Delete**: the reply's `deleteShown` equals the count in the title and the change report's deleted count. **FAIL:** the doors missing from the count, or listed without *goes with* (then say whether `GetDependentElements` answered, from the add-in log) |
| **HL4** | *Don't delete.* As HL1, press **Don't delete** | Nothing gone - the walls are there and selected as before the request, the view where it was - and **no** new undo entry. The chat's reply is a refusal that says the modeller pressed DON'T DELETE and NOTHING WAS DELETED, and the chat does not try again |
| **HL5** | *Closing the box.* As HL1, press **Esc**; ask again and close the box with its **X** | Each time: nothing gone, no new undo entry, the reply says the modeller *closed the box, which deletes nothing* |
| **HL6** | *A change that deletes nothing.* With Ask to Keep off, ask for one change that deletes nothing - `CREATE_LEVEL`, or a Mark set on one door | **No box of any kind**; kept at once, one undo entry, and the reply has no `deleteAsked` |
| **HL7** | *Too late.* Set `revit.askTimeoutSeconds = 60` in `heron.config`, restart Revit, ask as HL1 and wait until the time the box says has passed, then press **Delete** | Nothing gone; the add-in log says the modeller answered `late`; the chat was told *nothing is deleted now* (`awaiting_delete_answer`) or that the answer came too late. Put the setting back |
| **HL8** | *A change that deletes as part of its work.* On the same scratch copy, one of the PROVEN capabilities D-142 lists - `REMOVE_TAGS` on a tagged door, say | The delete box is up, listing what it removes; Delete keeps it as one undo entry |
