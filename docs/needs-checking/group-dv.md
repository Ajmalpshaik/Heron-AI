# Needs checking — Group DV

> One group of [the register](../NEEDS-CHECKING.md), in its own file since 2026-10-13 so that it can be read
> alone. **The register's rules, and every group's place in it, are on that page.** A new row
> for this group goes in this file. [`tools/needs-checking-register.py`](../../tools/needs-checking-register.py)
> reads it back into the register for every tool that reads the register, so a row here is seen
> exactly as it was seen there.

## Group DV - door tags in a plan, a sub-category switched in a view, and which side of a wall its dimensions go (2026-10-13)

Three widenings found making the futuristic house's drawing set in Project2 (Revit 2020.2), all three run on
Revit 2020.2 in Project1 (session 18392, an unsaved scratch project - Project2 was closed without saving at
14:42), on a duplicate plan, `Tool Test - 6e9a05`, with a negative case each.

**What changed.** `TAG_ELEMENTS_IN_VIEW` version 4 tags every taggable category, not only MEP
([FRAGMENT-ISSUES 5b-450](../fragment-issues/section-5b-rows-401-425.md)). `DIMENSION_WALL_OPENINGS` version 2
takes `side` - outside, inside, left, right, or blank for version 1's side ([5b-451](../fragment-issues/section-5b-rows-401-425.md)).
The add-in's binder resolves a sub-category - *"Rooms: Interior Fill"*, or a bare name only one category has -
for a need whose card says `subcategories: true`, which today is `SET_CATEGORY_VISIBILITY`'s `categories`
alone; `SET_CATEGORY_GRAPHICS` and `SET_CATEGORY_SOLID_FILL` stay top-level, for the reason in
[5b-453](../fragment-issues/section-5b-rows-401-425.md). That closes 5b-448 (PR #509) once both are merged.

**What is proved and what is not.** Both fragment changes ran in Revit, were kept on the test plan and read
back. `TAG_ELEMENTS_IN_VIEW`'s version 4 draft matched the code until version 5 ([5b-452](../fragment-issues/section-5b-rows-401-425.md)) changed it, so DV5's draft replaces it.
`DIMENSION_WALL_OPENINGS`' draft does NOT: a review of the PR found four faults in the side choice, fixed
after Revit had been closed - compiled, not run (5b-451) - so DV2 is one more rolled-back run first. The binder change is compiled for all
eight releases and **has not run in Revit**: the add-in in the running Revit is the one it loaded, and it
refused both *"Rooms: Interior Fill"* and *"Interior Fill"* "No category called ...", as on the house. A
rolled-back probe carrying the new binder methods word for word resolved *"Rooms: Interior Fill"* to
-2000161 and switched it hidden to shown with Rooms left as it was, switched *"Walls: Common Edges"*, and
refused *"Interior Fill"* naming Areas, HVAC Zones and Rooms - so Revit's half is measured, and the compiled
binder's is DV3.

With `HERON_CLIENT_ID=ajmal-pc` and `< /dev/null` on every line. The two drafts and their run records are
git-ignored, so they are not in this change: they sit in the worktree that made them
(`.claude/worktrees/vigorous-ritchie-8b204c/brain/proof-drafts/`) and a copy in
`%TEMP%\heron-work\ritchie-6e9a05\keep`. The main checkout's `brain/proof-drafts/runs/` already holds the
September records of version 3 and version 1 under the same two names - move those aside before copying
these in to sign there, because `accept` reads the record from that one place. **Since version 5
([5b-452](../fragment-issues/section-5b-rows-401-425.md)) only `dimension-wall-openings`' pair is still to be
copied from there:** the `tag-elements-in-view` pair is version 4's, and copied over DV5's it leaves a draft
`accept` refuses - its fingerprint is not the card's.

| # | Run | Look for |
|---|---|---|
| **DV1** | ~~Read [`brain/proof-drafts/tag-elements-in-view.yaml`](../../brain/proof-drafts/README.md), then `python brain/heron_validate.py accept tag-elements-in-view --by "Ajmal PS"` and set `heron-status: PROVEN`~~ **SUPERSEDED by DV5** - version 5 changed the code under this draft, and `accept` refuses a draft whose fingerprint is not the card's. Sign DV5's instead | ~~positive: 4 doors tagged by *M_Door Tag (the caller's hint)*; negative: the 4 rooms, tagged 0, sent to `TAG_ROOMS`; fingerprint `1178953aaf3a21e6`~~ - the door evidence stays in [5b-450](../fragment-issues/section-5b-rows-401-425.md) |
| **DV2** | On a scratch model: a box of walls drawn clockwise, two rooms south of the north wall, a door in it, a duplicate plan. `validate --in <title> dimension-wall-openings --write --setup select-in-region --keep-chain` (the north wall), positive `side=outside`, negative `side=outsid`, `offsetMm=900`, `withOverall=true`; then `draft` and `accept --by "Ajmal PS"` | positive: 2 strings, *"above the wall on the view (outside: a room on one side only - outside is the side without it)"*; negative: refused, created 0; fingerprint `7dd9a387271437a7` - the ef524f3e98e95e3d draft is of the code before the review |
| **DV3** | After the add-in from this branch is deployed and Revit restarted, on a scratch plan: `SET_CATEGORY_VISIBILITY` with `categories=Rooms: Interior Fill`, `visible=true`; then `categories=Interior Fill`; then `SET_CATEGORY_GRAPHICS` with `categories=Rooms: Interior Fill` | changed 1 and the box in Visibility/Graphics ticked with Rooms as it was; the second refused naming Areas, HVAC Zones and Rooms; the third refused "No category called" - the binder keeps sub-categories from a card that did not ask for them |
| **DV4** | Revit 2024 or later, a scratch plan: set a projection-line override on *Rooms: Interior Fill* through the Visibility/Graphics dialog, then read it with a probe's `GetCategoryOverrides` | whether a sub-category's override is stored and readable on a later release - [5b-453](../fragment-issues/section-5b-rows-401-425.md)'s unmeasured half |
| **DV5** | **DONE 2026-10-13** - signed by Ajmal PS after PR #519 merged, and promoted to PROVEN. ~~The owner's word on [5b-452](../fragment-issues/section-5b-rows-401-425.md) - given 2026-10-13: the head on the element. Version 5 was run on Revit 2020.2, Project1 session 33484, and each head read back on its duct (5b-452). Read `brain/proof-drafts/tag-elements-in-view.yaml` - git-ignored, so copy it AND its run record `brain/proof-drafts/runs/tag-elements-in-view.json` (`accept` re-reads both) from `.claude/worktrees/fervent-jang-7a1d36/brain/proof-drafts/` or `%TEMP%\heron-work\fervent-7a1d36\keep`, moving the main checkout's older pair of the same names aside first - then `python brain/heron_validate.py accept tag-elements-in-view --by "Ajmal PS"` and set `heron-status: PROVEN`~~ | positive: 3 ducts tagged by *M_Duct Size Tag (the caller's hint)*; negative: the same ducts with `minLengthMm=100000`, tagged 0, tooShort 3; fingerprint `12bbc94345a1dc3d` |
| **DV6** | Read [`brain/proof-drafts/set-category-graphics.yaml`](../../brain/proof-drafts/README.md), then `python brain/heron_validate.py accept set-category-graphics --by "Ajmal PS"` and set `heron-status: PROVEN` | version 2 ([5b-453](../fragment-issues/section-5b-rows-401-425.md)), run 2026-10-13 on Project1, Revit 2020.2, rolled back: positive Walls, line 200,30,30 weight 5, `overridden 1`; negative Rooms, `overridden 0`, refused *"Revit took the override and stored nothing for Rooms"*; second route `REPORT_CATEGORY_OVERRIDES`; fingerprint `b1d6f1eeb854fcba` |
| **DV7** | Read [`brain/proof-drafts/set-category-solid-fill.yaml`](../../brain/proof-drafts/README.md), then `python brain/heron_validate.py accept set-category-solid-fill --by "Ajmal PS"` and set `heron-status: PROVEN` | version 2, the same run: positive Walls with the solid fill read back on surface and cut, `overridden 1`; negative Rooms, `overridden 0`, the same sentence; fingerprint `d8d8e5d89b509e2a` |

**DV6 and DV7, where the drafts are.** Git-ignored, so not in the change that made them: in the worktree that
ran them (`.claude/worktrees/vigorous-nash-ab470b/brain/proof-drafts/`), in `%TEMP%\heron-work\5b-453\keep`
with the rolled-back read-backs and version 1's runs, and copied into the main checkout's
`brain/proof-drafts/` - its September `runs/set-category-graphics.json` (version 1, Snowdon, Revit 2024) moved
aside to `set-category-graphics.2026-09-15-v1.json` first. Sign after the change is merged, so the card on
`main` is version 2: `accept` refuses a draft whose fingerprint is not the card's.
