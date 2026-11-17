# NEEDS-CHECKING archive — Group DV

> **Checks that were done, moved out of the live register.** These are rows of Group DV of
> [`NEEDS-CHECKING.md`](../NEEDS-CHECKING.md) — *door tags in a plan, a sub-category switched in a view, and which side of a wall its dimensions go (2026-10-13)* — whose ID was struck through, the register's own
> sign that the check was done. Each still has its line in the register, with a link here. **Its words
> are unchanged; only its links were re-pointed.** Nothing new is written here: a new check goes in the
> register. Written by [`tools/archive-needs-checking.py`](../../tools/archive-needs-checking.py).

---
### Row DV1

*Moved from the register on 2026-11-17.*

**Run.** ~~Read [`brain/proof-drafts/tag-elements-in-view.yaml`](../../brain/proof-drafts/README.md), then `python brain/heron_validate.py accept tag-elements-in-view --by "Ajmal PS"` and set `heron-status: PROVEN`~~ **SUPERSEDED by DV5** - version 5 changed the code under this draft, and `accept` refuses a draft whose fingerprint is not the card's. Sign DV5's instead

**Look for.** ~~positive: 4 doors tagged by *M_Door Tag (the caller's hint)*; negative: the 4 rooms, tagged 0, sent to `TAG_ROOMS`; fingerprint `1178953aaf3a21e6`~~ - the door evidence stays in [5b-450](../fragment-issues/section-5b-rows-426-450.md) - **Struck 2026-10-18:** CLOSED, not passed - superseded by DV5, which was signed (`tag-elements-in-view` PROVEN, fingerprint `12bbc94345a1dc3d`).

---

### Row DV5

*Moved from the register on 2026-11-17.*

**Run.** **DONE 2026-10-13** - signed by Ajmal PS after PR #519 merged, and promoted to PROVEN. ~~The owner's word on [5b-452](../fragment-issues/section-5b-rows-451-475.md) - given 2026-10-13: the head on the element. Version 5 was run on Revit 2020.2, Project1 session 33484, and each head read back on its duct (5b-452). Read `brain/proof-drafts/tag-elements-in-view.yaml` - git-ignored, so copy it AND its run record `brain/proof-drafts/runs/tag-elements-in-view.json` (`accept` re-reads both) from `.claude/worktrees/fervent-jang-7a1d36/brain/proof-drafts/` or `%TEMP%\heron-work\fervent-7a1d36\keep`, moving the main checkout's older pair of the same names aside first - then `python brain/heron_validate.py accept tag-elements-in-view --by "Ajmal PS"` and set `heron-status: PROVEN`~~

**Look for.** positive: 3 ducts tagged by *M_Duct Size Tag (the caller's hint)*; negative: the same ducts with `minLengthMm=100000`, tagged 0, tooShort 3; fingerprint `12bbc94345a1dc3d` - **Struck 2026-10-18:** done, as it says - `tag-elements-in-view` PROVEN, fingerprint `12bbc94345a1dc3d` in its `fragment.yaml`.

---

### Row DV6

*Moved from the register on 2026-11-17.*

**Run.** ~~Read [`brain/proof-drafts/set-category-graphics.yaml`](../../brain/proof-drafts/README.md), then `python brain/heron_validate.py accept set-category-graphics --by "Ajmal PS"` and set `heron-status: PROVEN`~~

**Look for.** **DONE 2026-10-13** - signed by Ajmal PS, the card PROVEN. Was: version 2 ([5b-453](../fragment-issues/section-5b-rows-451-475.md)), run 2026-10-13 on Project1, Revit 2020.2, rolled back: positive Walls, line 200,30,30 weight 5, `overridden 1`; negative Rooms, `overridden 0`, refused *"Revit took the override and stored nothing for Rooms"*; second route `REPORT_CATEGORY_OVERRIDES`; fingerprint `b1d6f1eeb854fcba`

---

### Row DV7

*Moved from the register on 2026-11-17.*

**Run.** ~~Read [`brain/proof-drafts/set-category-solid-fill.yaml`](../../brain/proof-drafts/README.md), then `python brain/heron_validate.py accept set-category-solid-fill --by "Ajmal PS"` and set `heron-status: PROVEN`~~

**Look for.** **DONE 2026-10-13** - signed by Ajmal PS, the card PROVEN. Was: version 2, the same run: positive Walls with the solid fill read back on surface and cut, `overridden 1`; negative Rooms, `overridden 0`, the same sentence; fingerprint `d8d8e5d89b509e2a`

---
