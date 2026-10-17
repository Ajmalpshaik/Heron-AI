# Needs checking — Group D

> One group of [the register](../NEEDS-CHECKING.md), in its own file since 2026-09-23 so that it can be read
> alone. **The register's rules, and every group's place in it, are on that page.** A new row
> for this group goes in this file. [`tools/needs-checking-register.py`](../../tools/needs-checking-register.py)
> reads it back into the register for every tool that reads the register, so a row here is seen
> exactly as it was seen there. Written by
> [`tools/split-needs-checking.py`](../../tools/split-needs-checking.py).

## Group D — the move itself

**On a scratch model. Not a real project.** The move first ran on 2026-09-07 - three ducts up 200 mm in
Revit 2024 under [B8](group-b.md), driven from a fresh process rather than a chat - and nobody measured
it. **All six rows below were run from a chat and measured on 2026-10-17** - the run is written up under
the table. This line said *"Nothing below has ever run"*, and then *"D3 to D6 have never run"*, until
then.

**If `revit_apply_move` answers *"There is nothing waiting to be approved"*, the MCP server is running old
code** - restart it before D1. That trap, and the one about pinning `HERON_CLIENT_ID` when driving from
a command line, are both written up in [Group B](group-b.md). Since 2026-10-17 the server says so itself
on every answer and refuses the move while its code is old ([Group EU](group-eu.md)) - built, never seen
in a real chat, so restart it first anyway.

Checked without Revit on 2026-10-17: `tests/test_kernel.py` (`HeronUnits`, the millimetre-to-foot
conversion the move uses) and `tests/test_write_safety.py` (the chat half of the rails) pass. **That is
the code, not the model, and it does not close D3.**

| ID | Do this | Pass looks like |
|---|---|---|
| ~~**D1**~~ | ~~*"move the ducts up 200 mm"*~~ | **PASSED 2026-10-17.** The preview read *"This would move 3 ducts up 200 mm in Heron D3 move test. 725 would be skipped - 725 pinned."* - the 3 free ducts the arrangement left, and every pinned one named as skipped. Read straight after it: all four test ducts still at **3079 mm**. Nothing moved |
| ~~**D2**~~ | ~~Say yes~~ | **PASSED 2026-10-17.** Ajmal PS said yes in the chat; `revit_apply_move` answered *"Moved 3 ducts up 200 mm in Heron D3 move test. 725 were skipped - 725 pinned."* |
| ~~**D3**~~ | ~~**MEASURE ONE.**~~ | **PASSED 2026-10-17 - all three measured, and each is +200.0000 mm.** End-point Z **3173.1717 -> 3373.1717 mm** and Middle Elevation **3079 -> 3279 mm** on ducts 892432, 892448 and 892476; X, Y and length unchanged to 0.0001 mm. A second route agrees: the PROVEN `REPORT_BOUNDING_BOX` over the same three read `minZ` **9.75450044907594 -> 10.4106684280786 ft**, which is 0.65616798 ft = **200.0000 mm**. Not 200 feet, not 0.656 of anything. **The negative held:** the pinned control duct 892462, drawn beside them, and an original pinned duct 392168 did not move at all |
| ~~**D4**~~ | ~~Look at the screen without clicking or zooming~~ | **PASSED 2026-10-17, measured on the window, not judged by eye.** Revit's own window was captured (PrintWindow, nothing else on the screens) in `{3D}` before the move and straight after it, nobody clicking between. The three ducts moved **24 px further up** the picture than two air terminals that were not moved. The whole picture had also shifted 51 px between those two frames - the first was taken a second after Heron zoomed to the ducts, so the camera was still settling; not measured further. After the undo the check is clean: the air terminals moved **0 px** and the ducts came down **24-28 px** |
| ~~**D5**~~ | ~~**One** Ctrl+Z~~ | **PASSED 2026-10-17.** Ajmal PS opened Revit's Undo list: **one** entry, *"Heron: move ducts up 200 mm"*, above his own *"Pin"* and *"Duct"* steps. **One** Ctrl+Z, and all three read **3079 mm** again with the 725 pins still in place. He also pressed Redo by accident: back to 3279, and a second single Ctrl+Z put them back again |
| ~~**D6**~~ | ~~*"move them down 50 mm"*~~ | **PASSED 2026-10-17.** The preview read *"3 ducts down 50 mm"*; after his yes, all three went **3079 -> 3029 mm (-50.0000 mm)**, the pinned control stayed at 3079, and the undo entry was named *"Heron: move ducts down 50 mm"* - a direction, not "up -50". One Ctrl+Z put them back to 3079 |

### The run that closed D1 to D6 - 2026-10-17

**Who and where.** Run from a chat by Claude for Ajmal PS, who watched in Revit, answered every preview
and pressed every undo. Revit **2024.3**, session 8168, add-in **0.1.0+76006767** (main at #571 - the
move's own files, `RevitWrite.cs` and `HeronUnits.cs`, are byte-identical on main at #563). The live
MCP server had started after that add-in, so the *"nothing waiting to be approved"* trap did not apply.

**The model.** `Heron D3 move test` - a copy of Autodesk's `rme_basic_sample_project.rvt` from Revit
2020's Samples folder, opened in Revit 2024 from `%TEMP%\heron-work\d3-move-7d0fb8\`, metric, 724 ducts,
**never saved**. Not a real project.

**The move does not read the selection, and the arrangement had to allow for it.** `revit_preview_move`
takes **every** element of the category in the whole model and skips only pinned, owned and similar ones
(`RevitWrite.Partition`). So *"the unselected duct must not move"* cannot be arranged by selecting. Ajmal
PS pinned all 724 original ducts, drew four free-standing ducts by hand and pinned one of them as the
control: the move then had exactly three to take, and two pinned ducts to leave alone. **He arranged it
by hand because Heron could not:** the first arranging call, a PROVEN `CREATE_DUCT` through
`revit_change`, failed before anything was kept - the D-126 Keep or Discard box threw *"Corresponding
button not found. Parameter name: defaultButton"* - and the model read 21,562 elements before and after.
That is a separate defect - [row 5b-621](../fragment-issues/section-5b-rows-401-425.md), fixed in #614 and not yet run in Revit; the move does not use that box.

**The tape measure.** A read-only probe sent through the bridge under `find-views`' READ contract - no
transaction, the element count read the same before and after every one - printed, for each duct, its
end points and length from the curve, and `Middle Elevation`, `Bottom` and `Top` from Revit's own
parameters, in millimetres from internal feet. It was run nine times: before, after the preview, after the
move, after each undo, after the redo, after "down 50 mm". The second route was the PROVEN
`SELECT_BY_PIN_STATE` -> `REPORT_BOUNDING_BOX` chain over the three free ducts, before and after. The
audit log's entry for the move (workflow `wf-20261017-121011-0039-0ba5`) names exactly the three
UniqueIds as moved and the 725 pinned as skipped.

**The staleness fingerprint** is the golden library's: G-09 (D1), G-10 (D3, D6), G-11 (D5) and G-17
(the audit) in [`tests/golden/cases.py`](../../tests/golden/cases.py) are PROVEN against
`RevitWrite.cs`, `HeronUnits.cs` and `HeronAudit.cs` as they are on main today, and
`python tests/test_golden.py` reports them STALE the moment any of those files changes.

**What this does NOT prove.** One release, Revit 2024. Ducts only, and only free ones - no duct joined
to a fitting was moved, so what the move does to a connected run is unmeasured. A model that is not
workshared. A failure forced in the middle of a move (golden G-12), an expired preview (G-13), a model
that changed after the preview (G-14) and a preview approved in the wrong model (G-15) were not run here.
And whether `write.enabled` should be on by default is D-19's question and the owner's decision -
this run answers what a move does, not whether Heron should be allowed to make one.

**The owner's answer, the same day.** Asked whether `write.enabled` should now be on by default, Ajmal PS answered from his phone: *"See these changes and admin and publish on off. I can do it from here with one click no problem."* Read as: the three ribbon switches - Changes, Admin, Publish - stay his to turn on and off with one click, so **the default is left as it is, `false`**, and [D-19](../DECISIONS.md) is unchanged. His own switch was already on, and stays as he set it.
