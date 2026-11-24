# FRAGMENT-ISSUES archive — section 5b, rows 151 to 175

> **Finished rows, moved out of the live register.** These are rows from section 5b of
> [`FRAGMENT-ISSUES.md`](../FRAGMENT-ISSUES.md) — Heron's own defects found by reading the repository — whose state said they were
> finished when they were moved. Each still has its line in the register: the same number, a
> one-line title, the opening of its state, and a link to its full text here.
>
> **Nothing below was rewritten** except its links, which were re-pointed one folder deeper and
> each checked to reach the same file it reached before. Where a row disagrees with
> [DECISIONS.md](../DECISIONS.md), the [Golden Rules](../14-golden-rules.md) or the
> [Constitution](../../HERON_CONSTITUTION.md), **those win** — a row records what was true on
> the day it was written.
>
> **Nothing new is written here.** A new defect goes in the register, and a row here that turns
> out not to be finished is reopened **there**, on its own line.
>
> Rows 151 to 175 of section 5b belong in this file. A number in that band with no entry below
> was never moved — it is still in the register, in full. Written by
> [`tools/archive-fragment-issues.py`](../../tools/archive-fragment-issues.py).

---

### Row 5b-159

*Moved from the register on 2026-11-17.*

**Defect.** **A FRAGMENT WRITE'S AUDIT LINE DOES NOT SAY WHAT REVIT RAISED.** Found 2026-09-23 (package C3). The move path's own audit record has carried `warnings` since it was written. From 2026-09-23 a fragment write's REPLY carries `warnings` and `warningsDismissed` too - but a fragment run's audit line is written by [`RevitDispatcher.cs`](../../revit/Heron.Revit.Addin/RevitDispatcher.cs) from the request and the reply's `error` and `document`, so the trail that exists to answer *"what happened to my model"* records that a fragment ran and not that Revit warned about what it did. The chat hears it; the trail does not. **Not fixed here:** the dispatcher is outside C3's files. The repair is one field: read `warnings` off the response beside `error`, for the two fragment operations.

**State.** **MOVED TO ISSUE [#898](https://github.com/Ajmalpshaik/Heron-AI/issues/898) 2026-11-16** - still open, tracked there; this row is kept whole. Was: **OPEN - recorded, not repaired** **2026-10-09:** FIXED IN SOURCE, NOT YET SEEN in a real Revit's audit file. [`Json.cs`](../../revit/Heron.Bridge/Json.cs) has `ReadLong`, which walks the reply the way `ReadString` does and returns the top-level whole number, or null for absent, a string, a fraction, an exponent, overflow or a malformed reply. [`RevitDispatcher.cs`](../../revit/Heron.Revit.Addin/RevitDispatcher.cs) adds `warnings` to the audit line's numbers, beside `ms`, for the four `run_fragment_*` operations; one `fragmentRun` test decides both this count and the `fragment` name. An absent count stays absent, never written as 0, because a read or a refusal says nothing about warnings. **Evidence:** `tests/test_failure_note.py`. Section 3 reads the dispatcher and the executor as text: the count is read as a number, added to the list the audit line is written from, only when the reply carried one, for fragment runs only, and the verdict writes it with `Json.Num`. Section 5 runs `Heron.FailureNote.TestHost`, which now links `Json.cs` by source and reads a reply built the way `WithVerdict` builds it, with a decoy `warnings` nested under `provides`. Seen to fail 2026-10-09: with `Json.cs` and the dispatcher as they stood, five dispatcher checks fail and the host does not build (no `ReadLong`); with only the dispatcher as it stood, the five fail and the host passes. Three narrower breaks - the count added to a list the line is not written from, a 0 for a reply that carried none, the verdict writing the count as text - each fail one check; the first version of the suite passed all three, and was tightened. `check-compile` builds every project for 2020-2027. **Still owed, so still OPEN:** a fragment write Revit warns on, run on the owner's PC, whose audit line carries `"warnings": N`, and a read whose line carries no `warnings` key.

---

### Row 5b-161

*Moved from the register on 2026-11-17.*

**Defect.** **ARTICLE 7'S "EXPLICIT CONFIRMATION FOR THAT SPECIFIC OPERATION" HAS NO CODE BEHIND IT ON `revit_change`.** Found 2026-09-23 while recording [D-99](../DECISIONS.md#d-99--a-change-asked-for-in-a-chat-is-kept-at-once-with-no-preview-and-article-9-says-so). Article 7 asks for explicit confirmation before deleting elements, purging, bulk parameter writes, family reloads, workset changes and anything touching a linked model, and the Constitution's Enforcement table placed it at *"Revit add-in permission gate + transaction wrapper"*. That gate reads one risk per OPERATION - `run_fragment_write` is `MODIFY` in [`HeronOperationRegistry.cs`](../../platform/Heron.Core/HeronOperationRegistry.cs) - plus the Changes switch, and never the fragment. So with Changes ON, `revit_change` runs `DELETE_ELEMENTS`, `UNLOAD_LINKS`, `RELOAD_LINKS`, `RENAME_WORKSET` and `SET_ELEMENT_WORKSET` - every one `risk: MODIFY` - and nothing asks: `grep -n "TaskDialog\|onfirm"` over `RevitFragment.cs`, `RevitOperations.cs`, `RevitDispatcher.cs` and `HeronPermissions.cs` finds nothing, and the server sends straight through. **Not introduced by D-99** - the path has run this way since 2026-09-15; D-99 is what wrote it down. **What carries Article 7 now is text:** every chat's instructions include it (`host.chat`), which is guidance a model can be argued out of - the Constitution's own warning about a rule stated and not enforced. **What was NOT checked:** which of the library's `MODIFY` fragments are destructive in Article 7's sense. No field on a fragment says so, and that missing field is the likely shape of any repair - a declared `destructive:` the server reads before sending, or a question the add-in asks before it opens the transaction group.

**State.** **MOVED TO ISSUE [#910](https://github.com/Ajmalpshaik/Heron-AI/issues/910) 2026-11-16** - still open, tracked there; this row is kept whole. Was: **OPEN - needs the owner**: should a destructive fragment through `revit_change` stop and ask, as Article 7 says, or be recorded as a second exception? The Enforcement table now says the switch is all that stands there. **Deleting elements answered 2026-10-18** by the owner's [D-142](../decisions/D-142.md) ([row 5b-1200](../fragment-issues/section-5b-rows-1176-1200.md)): any kept change Revit counts a deletion in is asked in Revit's own box, by measurement, not by a field on the card - built, not run in Revit. The rest of Article 7 - purging, bulk parameter writes, family reloads, worksets, links - stays OPEN.

---

### Row 5b-175

*Moved from the register on 2026-11-17.*

**Defect.** **THREE FRAGMENTS ANSWER "IS IT IN THE ROOM" TWO DIFFERENT WAYS, AND ONE CARD SAYS THE OPPOSITE OF ITS OWN CODE.** Found 2026-09-23 checking the plan's row 5, which the plan marked *reported*: a device in the ceiling void is outside its room's VOLUME (observed elsewhere, mostly on Revit 2020). In the files: `count-by-spatial-container` tests the element's X and Y at the container's own Z (`impl/any/fragment.cs:84-88`), so a ceiling-void diffuser counts IN the room below; `check-room-mep-completeness` calls `room.IsPointInRoom(point.Point)` at the device's own insertion point (`:94-99`) - the volume test - while its purpose says *"one in the ceiling void above the room still counts"*; `filter-elements-in-room` asks the volume test too (`:65-71`), and its purpose says an element above the ceiling *"can be genuinely inside"*. Where a room's volume stops at the ceiling, the first counts the diffuser in and the other two leave it out. **AND THE SIGNED PROOF OF `check-room-mep-completeness` NEVER REACHED THE ROOM TEST.** Its positive handed in 8 ducts as the devices; a duct has a location CURVE and the code counts only a location POINT (`:94-95`), so every room came back *"short of: Ducts (0 of 1)"* whatever the model held. **Not checked**: which answer Revit gives on a real room - that needs a model.

**State.** **MOVED TO ISSUE [#923](https://github.com/Ajmalpshaik/Heron-AI/issues/923) 2026-11-16** - still open, tracked there; this row is kept whole. Was: **OPEN - recorded, not settled.** The plan says settle each with evidence, not by picking one: all three cards carry a note pointing here, and NEEDS-CHECKING AJ5 is the arrangement. The proof is the owner's signed record and is not touched - whether to re-take it with point-based devices is the owner's call **RUN 2026-09-23 on the owner's PC - [NEEDS-CHECKING AJ5](../needs-checking/group-aj.md):** exactly as the code predicts - with the room's top at the ceiling, `count-by-spatial-container` counted the void diffuser IN and the other two OUTSIDE; with its Upper Limit at the level above, all three inside. Which meaning a room should have is the owner's; asked once, not answered.

---
