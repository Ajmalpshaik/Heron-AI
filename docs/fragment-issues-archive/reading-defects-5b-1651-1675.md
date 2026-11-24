# FRAGMENT-ISSUES archive — section 5b, rows 1651 to 1675

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
> Rows 1651 to 1675 of section 5b belong in this file. A number in that band with no entry below
> was never moved — it is still in the register, in full. Written by
> [`tools/archive-fragment-issues.py`](../../tools/archive-fragment-issues.py).

---

### Row 5b-1651

*Moved from the register on 2026-11-17.*

**Defect.** **WHAT A PLAN SHOWS, READ THROUGH THAT PLAN, CHANGED IN THE MIDDLE OF ONE JOB WITH NOTHING ADDED TO OR TAKEN FROM THE MODEL - 258 DUCTS BEFORE THE TAGS WERE PLACED, 557 AFTER - AND ONLY 90 OF THE 244 DUCT TAGS JUST MADE READ AS SHOWN IN IT.** Seen 2026-11-14 on the owner's PC running Group NR's NR1 (`annotate-a-view`). Revit 2020, session 30228, the scratch copy *P1102 MEP NR*, *Level 1 HVAC Duct Plan* (1:100, its own settings, no view template, Phase Filter *Show All*, NR4's four colour filters on it). `FILTER_ELEMENTS_BY_CATEGORY` with `inViewOnly` that plan read *Ducts 258* in NR4 (model 21,463 elements) and again at NR1's first step (*Ducts 258, Air Terminals 105*, model 21,467); `TAG_ELEMENTS_IN_VIEW` then answered `tagged 349` (*Duct Tags 244, Air Terminal Tags 105*), `notTagged 0`, model 21,816. After two kept `ARRANGE_TAGS` calls, the same filter read *Ducts 557, Air Terminals 105, Duct Tags 90, Air Terminal Tags 105* - model still 21,816 - and read the same twice; `FIND_UNTAGGED_ELEMENTS` answered `elements 467`, `alreadyTagged 195`; `REPORT_TAGS_AND_TARGETS` `tagCount 195`, `orphanTags 0`. The duct-tag filter between the two arrange calls read 111. `DIAGNOSE_VISIBILITY` on two of the duct tags not shown (1020385, 1020369) answered only *"4 view filter(s) are applied and one may hide it"* - NR4's filters, which set colour only. The owner saw no Temporary Hide/Isolate border on the plan; his whole-plan screenshot was too small to count tags by. **It flips back and forth.** After the owner opened the plan and zoomed it to fit, the same filter read *Ducts 258* again, `FIND_UNTAGGED_ELEMENTS` `elements 14` (the risers) and `alreadyTagged 349`, the Duct Tags filter 244, and `FIND_OVERLAPPING_TAGS` compared *349 annotation element(s)*. After the next change the owner kept at a Keep box (NR3's `ARRANGE_TAGS`), it read *Ducts 586, Duct Tags 81* and the overlap read compared *186*. Then the owner zoomed the plan out in Revit (a click and a zoom, nothing changed): the filter read *Ducts 258, Duct Tags 244* again. Every overlap read taken straight after a Keep box compared 185 to 195 of the 349 tags. NR1's `ARRANGE_TAGS` was therefore handed 111 of the 244 duct tags and never saw the rest.

**State.** **MOVED TO ISSUE [#935](https://github.com/Ajmalpshaik/Heron-AI/issues/935) 2026-11-16** - still open, tracked there; this row is kept whole. Was: **OPEN - recorded, not explained.** The pattern seen: the plan read through Heron loses tags and gains ducts after a change kept at a Keep box, and comes back after the owner opens or zooms the plan - whether the click or the zoom does it was not separated. Whether the Keep box's own select-and-isolate step leaves the plan in that state, and which reading Revit itself would give (a box over the plan, then Filter), was not found. Until it is, a find run straight after a kept change can hand the next step part of the view.
