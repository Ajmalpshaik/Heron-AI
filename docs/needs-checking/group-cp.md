# Needs checking — Group CP

> One group of [the register](../NEEDS-CHECKING.md), in its own file so that it can be read alone.
> **The register's rules, and every group's place in it, are on that page.** A new row for this group
> goes in this file. [`tools/needs-checking-register.py`](../../tools/needs-checking-register.py) reads
> it back into the register for every tool that reads the register.

## Group CP - nested families: a read run that left an element, formula Yes/No values, hidden bodies in a box, and how a work-plane family stands (2026-10-07)

Four things seen on 2026-10-06/07 building the nested `GM_PipeSupport_*` families for Rejin's pipe support
on Revit 2024 (session 51820). The drivers and full call logs are in `Desktop/For Rejin/Pipe Support/build/`
and `build/log/` on the owner's PC. Nothing here is fixed; each row is the run that would settle it.
**Run every row on a SCRATCH family**, never on a `GM_PipeSupport_*` file, and count the family's elements
before and after each run.

| # | Run | Look for |
|---|---|---|
| **CP1** | A scratch Generic Model family with a reference plane and one small Generic Model loaded. Count elements, then run `place-nested-family` for it WITHOUT `--write`; count again. Repeat once with `--write` and roll back | **The no-write run must leave the count unchanged** ([row 5b-350](../fragment-issues/section-5b-rows-326-350.md)). On 2026-10-06 it left one instance each time (57 where 56 were expected). Record whether a level host does the same as a reference-plane host |
| **CP2** | A family with a Yes/No `Flag`, a Yes/No `Derived = not(Flag)` and a Yes/No with no formula; `report-family-parameters` with Flag Yes, then No | **Each Yes/No should read Yes or No.** On 2026-10-06 every formula-driven Yes/No read `<blank>` ([row 5b-351](../fragment-issues/section-5b-rows-351-375.md)); record whether the no-formula one did too |
| **CP3** | A family holding two bodies far apart, each `Visible` linked to its own Yes/No, nested in a host with one switched off; `report-bounding-box` on the nested instance | **Record whether the box covers the hidden body.** On 2026-10-07 the C-channel read 200 x 1075 x 1100 mm with its horizontal and upright bodies both counted ([row 5b-352](../fragment-issues/section-5b-rows-351-375.md)) |
| **CP4** | A work-plane-based Generic Model with a body longer in its own Z, nested by `place-nested-family` on a VERTICAL reference plane in the host | **Record which way the body points.** On 2026-10-06 nested work-plane-based parts came in upright - the family's Z along the world's Z - so the pipe support's orientations were built as switchable bodies instead ([43 §9](../43-building-revit-families.md)). Whether that is Revit's rule or the fragment's choice of the plane's in-plane direction (`across` in `place-nested-family`) is not established |

The negative half of the lookup-table question - an ND that sits only in the first column - is **BV15**'s,
in [Group BV](group-bv.md), and is not repeated here.
