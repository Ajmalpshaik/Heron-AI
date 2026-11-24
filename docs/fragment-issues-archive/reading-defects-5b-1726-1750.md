# FRAGMENT-ISSUES archive — section 5b, rows 1726 to 1750

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
> Rows 1726 to 1750 of section 5b belong in this file. A number in that band with no entry below
> was never moved — it is still in the register, in full. Written by
> [`tools/archive-fragment-issues.py`](../../tools/archive-fragment-issues.py).

---

### Row 5b-1749

*Moved from the register on 2026-11-17.*

**Defect.** **`LIST_MATERIALS withAllTabs=true` THREW INSIDE REVIT 2024 - "Attempted to read or write protected memory" - ON A FRESH PROJECT'S GLASS.** Seen 2026-11-16, Revit 2024 (session 13912), `ny-scratch` made from `English\DefaultMetric.rte`, through `revit_read` with `namePattern=Glass*`, `withAllTabs=true` and every other flag false: *"'list-materials' threw while running: Attempted to read or write protected memory. This is often an indication that other memory is corrupt."* Revit stayed up and answered the next ping. The same call with `withAllTabs=false` answered: 2 materials, *Glass* (id 26) and *Glass, Clear Glazing* (id 123311). **Not yet known:** which read on the Appearance or Graphics tab throws - most likely an appearance asset read off a material whose asset is not loaded yet - nor whether it repeats.

**State.** **MOVED TO ISSUE [#949](https://github.com/Ajmalpshaik/Heron-AI/issues/949) 2026-11-16** - still open, tracked there; this row is kept whole. Was: **OPEN - recorded, not repaired.** `LIST_MATERIALS` is PROVEN; a fix moves its C#, so the card goes back to DRAFT and is the owner's call. Next: run `withAllTabs=true` again on the same model, then with one material at a time, to find the read that throws.

---

### Row 5b-1750

*Moved from the register on 2026-11-17.*

**Defect.** **OPEN_FAMILY_FOR_EDITING v2 REPORTS "NOT opened" AND "An internal error has occurred" - BUT THE FAMILY IS OPEN, IN FRONT.** NEEDS-CHECKING Group NY's NY3, Revit 2024 (session 13912), `ny-scratch`, 2026-11-16, through `revit_change` from main `05602ba2`, `familyName=M_Single-Flush`, `saveFolder` a scratch folder: `noTransactionOpen true`, `savedTo ...\M_Single-Flush.rfa` (the file written, 450,560 bytes), `opened false`, `problem "An internal error has occurred."`, findings *"... was written but Revit would not open it: An internal error has occurred."*. The chat's verdict began *"STOPPED, BY ITS OWN ACCOUNT"* (row 5b-1701's repair, seen working). **Yet Revit's main window read, by UI Automation, "Autodesk Revit 2024.3 - [M_Single-Flush - Floor Plan: Ground floor]"**, and the next call was pinned to `M_Single-Flush`. So row 5b-1701's first half holds - EditFamily no longer refused with *"The document is currently modifiable"* - but `OpenAndActivateDocument`, called inside the job's TransactionGroup, throws although the window opens (NY3's open question, answered).

**State.** **MOVED TO ISSUE [#920](https://github.com/Ajmalpshaik/Heron-AI/issues/920) 2026-11-16** - still open, tracked there; this row is kept whole. Was: **OPEN - recorded, not repaired.** The card is PROVEN; the fix moves its C# (read the document in front after the throw, or open the window after the group closes), so it goes back to DRAFT - the owner's call.

---
