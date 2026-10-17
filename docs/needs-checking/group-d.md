# Needs checking — Group D

> One group of [the register](../NEEDS-CHECKING.md), in its own file since 2026-09-23 so that it can be read
> alone. **The register's rules, and every group's place in it, are on that page.** A new row
> for this group goes in this file. [`tools/needs-checking-register.py`](../../tools/needs-checking-register.py)
> reads it back into the register for every tool that reads the register, so a row here is seen
> exactly as it was seen there. Written by
> [`tools/split-needs-checking.py`](../../tools/split-needs-checking.py).

## Group D — the move itself

**On a scratch model. Not a real project.** The move has run **once**, and not as these rows ask: on
2026-09-07 three ducts went up 200 mm in Revit 2024 under [B8](group-b.md), driven from a fresh process
rather than a chat. **Nobody measured them, and D3 to D6 have never run** - this line said *"Nothing below
has ever run"* until 2026-10-17.

**If `revit_apply_move` answers *"There is nothing waiting to be approved"*, the MCP server is running old
code** - restart it before D1. That trap, and the one about pinning `HERON_CLIENT_ID` when driving from
a command line, are both written up in [Group B](group-b.md).

Checked without Revit on 2026-10-17: `tests/test_kernel.py` (`HeronUnits`, the millimetre-to-foot
conversion the move uses) and `tests/test_write_safety.py` (the chat half of the rails) pass. **That is
the code, not the model, and it does not close D3.**

| ID | Do this | Pass looks like |
|---|---|---|
| **D1** | *"move the ducts up 200 mm"* | A **preview**. A count you can check by eye. **Nothing moves yet** |
| **D2** | Say yes | They move |
| **D3** | **MEASURE ONE.** | **200 mm.** Not 200 feet, not 0.656 of anything. **This is the single most important line in this file** — it is what catches a unit error, and it is the whole reason `HeronUnits` exists |
| **D4** | Look at the screen without clicking or zooming | The change is **visible**. That is `TryRefresh` |
| **D5** | **One** Ctrl+Z | Everything back, in **one** step, named "Heron: move ducts up 200 mm". If it takes two, Golden Rule 16 is broken |
| **D6** | *"move them down 50 mm"* | Negative distances work; down is a direction, not an error |
