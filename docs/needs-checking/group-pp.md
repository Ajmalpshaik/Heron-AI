# Needs checking — Group PP

> One group of [the register](../NEEDS-CHECKING.md), in its own file so that it can be read alone. **The
> register's rules, and every group's place in it, are on that page.** A new row for this group goes in
> this file. [`tools/needs-checking-register.py`](../../tools/needs-checking-register.py) reads it back
> into the register for every tool that reads it, so a row here is seen exactly as it was seen there.

## Group PP - a central model opened beside is not saved, a nested part kept upright says so, the session list names a wait, and the camera's eye on the sample house - BUILT, NOT RUN IN REVIT (2026-11-21)

**Tracked as GitHub issue [#1234](https://github.com/Ajmalpshaik/Heron-AI/issues/1234) since 2026-11-21** - the checks still owed are listed there; tick the group in that issue when they are done. Every result stays in this file.

**Why:** the open rows of section 5b that were never moved to a GitHub issue, worked on 2026-11-21 in a cloud
container with no Revit. Four were fixed in code there - rows
[5b-1833](../fragment-issues/row-5b-1833.md) and [5b-1834](../fragment-issues/row-5b-1834.md) (issue
[#1216](https://github.com/Ajmalpshaik/Heron-AI/issues/1216)), [5b-1837](../fragment-issues/row-5b-1837.md) (issue
[#1217](https://github.com/Ajmalpshaik/Heron-AI/issues/1217)), [5b-1838](../fragment-issues/row-5b-1838.md) (issue
[#932](https://github.com/Ajmalpshaik/Heron-AI/issues/932)) and part (2) of
[5b-1883](../fragment-issues/row-5b-1883.md) (issue [#902](https://github.com/Ajmalpshaik/Heron-AI/issues/902)) -
and one, [5b-1870](../fragment-issues/row-5b-1870.md), was repaired on 2026-11-20 with no run of its own written. The
add-in and every fragment compile for Revit 2020 to 2027 (`tools/check-compile.py`,
`tools/check-fragments-compile.py`), and `tests/test_save_after_group.py`, `tests/test_nested_upright.py`,
`tests/test_walled_own_words.py`, `tests/test_session_binding.py` and `tests/test_person_wait.py` hold the rest -
**none of it has met a Revit.** Rebuild the add-in and restart Revit first (`python tools/heron-ready.py`).

| Change | Files | Row | Checks |
|---|---|---|---|
| `SAVE_DOCUMENT` v4 (DRAFT): another open document that is a central model opened directly is refused in the run, and again before the add-in saves it | `save-document` `fragment.cs`, `RevitFragmentSave.cs` (`SaveOtherNow`) | [5b-1833](../fragment-issues/row-5b-1833.md) | PP1 |
| `PLACE_FAMILY_INSTANCES` v8 (DRAFT): a nested family kept upright on a plane that is not level says it does not stand on the plane, naming Always Vertical | `place-family-instances` `fragment.cs` | [5b-1883](../fragment-issues/row-5b-1883.md) (2) | PP2 |
| `info` says `leaseWaitingForModeller`; another chat's session list says *"waiting for an answer in Revit"* rather than a time | `HeronLease.cs` (`Waiting`), `BridgeServer.cs`, `heron_bridge_client.py` (`availability`) | [5b-1838](../fragment-issues/row-5b-1838.md) | PP3 |
| `FRAME_ELEMENTS_IN_CAMERA` v8 (DRAFT, 2026-11-20): every bottom level holding under a quarter of the busiest level's elements is passed over | `frame-elements-in-camera` `fragment.cs` | [5b-1870](../fragment-issues/row-5b-1870.md) | PP4 |
| A tool's own declared words, asked on a release it leaves out, name that tool whatever the ranking | `heron_brain.py` (`lookup`) | [5b-1837](../fragment-issues/row-5b-1837.md) | PP5 |

**The models: scratch copies, never a working model.** PP1 on a scratch workshared project made for it in
`%TEMP%\heron-work\pn` - a new project from the default template, worksharing enabled, saved as its own central -
with a second scratch project as the model Heron works in. PP2 in Group JB's scratch families. PP4 on a copy of
Revit 2020's `Samples\rac_basic_sample_project.rvt` and of *Heron Tower - First Build*, as row 5b-1870 used.

| ID | Do this | Pass looks like |
|---|---|---|
| **PP1** | *A central model opened beside is not saved - runs today, as a preview.* Changes and Publish on. Heron working in the second scratch project, in front; the scratch CENTRAL opened directly (not a local copy) behind it, with one wall drawn in it by hand and not saved. `heron_bridge_client.py fragment save-document --write --set documentPath=<the central's file>`, no `--apply` | Refused in the run, naming the central as *"the CENTRAL model itself, opened directly"*; `saveAsked false`, NO `save` block; the central file's time on disk UNCHANGED and its unsaved mark still there; no Synchronize with Central in Revit's journal. **Negative:** the same with a workshared LOCAL copy behind instead of the central - the `save` block `done true`, its words saying NOTHING WENT TO CENTRAL, the local file's time moving to now. Write down whether Revit raised any dialog |
| **PP2** | *A nested part kept upright says so.* Group JB's arrangement as it was BEFORE part (1) of row 5b-1883: *Heron JB Part.rfa* left Always Vertical yes, nested on `JB Front` with `PLACE_FAMILY_INSTANCES ... workPlane=JB Front`, rolled back | The read-back as before, *"nested on "JB Front" ... - read back"*, AND a finding that it is *"hosted on "JB Front" but kept upright"*, does not stand on it and has no work plane to flip, naming Always Vertical and `SET_FAMILY_SETTINGS always vertical=no`. **Negative:** the part set Always Vertical no (Group JB's arrangement now), nested the same way - no such finding, and `REPORT_BOUNDING_BOX` reads it standing on the plane; and any part nested on `Ref. Level` - no such finding |
| **PP3** | *The session list names a wait.* Two chats, one Revit. Chat A asks for a change with Ask to Keep on and leaves Revit's Keep or Discard box open; chat B asks `revit_health` (or is shown the session picker) | Chat B's line for that Revit reads *"(in use by another chat, waiting for an answer in Revit)"*, never *"~5 min left"*. Then Keep is pressed in Revit, and chat B asks again: *"~5 min left"* (or less). **Negative:** with no box open, chat B's line gives a time as before |
| **PP4** | *The camera's eye on the sample house, and the tower held.* `FRAME_ELEMENTS_IN_CAMERA` v8, rolled back, through `batch-prove` as row 5b-1870 ran it: (a) all 56 walls of the sample house selected, `shot=eye-level`; (b) and (c) the tower's two cases of the card's test plan (low-angle; version 2's road) | (a): readBack *"eye 1600 mm above 'Level 1'"*, and the finding naming 'Foundation' (1) and 'Level 1 Living Rm.' (1) as passed over; Revit's Eye Elevation 1600 mm above 'Level 1'. (b): eye 450 mm above 'Level 1', as on 2026-11-20. (c): *"11060 mm above 'Level 1'"*. **Negative:** the card's own negative cases - P1102's seven sheets refused, `noGeometry 7`, no view made. Then the card's proof draft may be written; it is signed by the owner only |
| **PP5** | *A PC run, no Revit needed* - on the owner's PC, on `model` (re-pointed 2026-11-26: this row asked for the re-ranker installed, `model+rerank`, and [D-201](../decisions/D-201.md) removed it), main against this branch | `python tests/test_walled_own_words.py` passes on the PC's backend too - every release-limited tool's own words, on every release it leaves out, name that tool. Write down the backend line `heron_lookup` prints. **Negative:** the same phrases on a release each tool IS declared for are answered by that tool by identity, nothing named beside it (the suite's third section) |
