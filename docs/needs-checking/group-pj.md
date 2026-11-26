# Needs checking — Group PJ

> One group of [the register](../NEEDS-CHECKING.md), in its own file so that it can be read alone. **The
> register's rules, and every group's place in it, are on that page.** A new row for this group goes in
> this file. [`tools/needs-checking-register.py`](../../tools/needs-checking-register.py) reads it back
> into the register for every tool that reads it, so a row here is seen exactly as it was seen there.

## Group PJ - COPY_ELEMENTS version 6: a copy by an offset asked with the offset alone, the three level values left out - DRAFT until proved (2026-11-21)

**Tracked as GitHub issue [#1201](https://github.com/Ajmalpshaik/Heron-AI/issues/1201) since 2026-11-21** - the checks still owed are listed there; tick the group in that issue when they are done. Every result stays in this file.

**Why:** on 2026-11-21, on the owner's PC (Revit 2024, the scratch project `roofjoin-oy`), copying one
roof 30 m along with `offset=0,30000,0` was refused `needs_request_values` until `toLevels`,
`turnDegrees` and `turnCentreMm` were typed blank - [row 5b-1949](../fragment-issues/row-5b-1949.md),
issue [#1175](https://github.com/Ajmalpshaik/Heron-AI/issues/1175). Version 6 marks the three
`optional: true` with `absent: ""`, so left out each binds as the blank text version 2 reads, and a value
given does what it did. Only the contract changed: impl/ is byte for byte what the 2026-11-02 proof (P1102
MEP, Revit 2020) ran, so its fingerprint still stands. The card is held at DRAFT at version 6 on the
coordinator's instruction for a contract change to a PROVEN card, with `proof-held` naming this group, so
`tools/check-signatures.py` reads it as a deliberate hold rather than a forgotten promotion.

| Tool | Was | Now | Row | Checks |
|---|---|---|---|---|
| [`copy-elements`](../../brain/fragments/copy-elements/fragment.yaml) (`COPY_ELEMENTS`) | PROVEN v5 | DRAFT v6, `proof-held` | [5b-1949](../fragment-issues/row-5b-1949.md) | PJ1 to PJ4 |

`tests/test_grid_and_copy_values.py` reads, through the mirror of the add-in's binder that the server asks
before it sends (`heron_fragment.need_may_be_absent`), that `offset` is now the one value asked for.
**None of it has run in Revit.** An empty `absent: ""` binds as empty text in the add-in since row
[5b-1841](../fragment-issues/row-5b-1841.md); that fix was in the add-in the owner ran on 2026-11-21
(0.2.0+c9af86d1), so no rebuild is needed for this card - only a chat opened on a checkout that has it.

**The model - Heron builds it, and it is nobody's work.** A new scratch project from Revit's default
template, on Revit 2024 (where the fault was seen) and then Revit 2020, with one basic roof by footprint on
Level 1 about 8 m deep - `roofjoin-oy`'s arrangement - saved under `%TEMP%\heron-work\pj`. Every run is
ROLLED BACK unless its row says KEPT. While the card is DRAFT, `revit_change` refuses it
([D-117](../decisions/D-117.md)): each run goes through the proving client - a rolled-back preview, or
`try-once` with the owner's Keep.

| ID | Do this | Pass looks like |
|---|---|---|
| **PJ1** | *Positive - the call refused on 2026-11-21.* The roof selected, `offset=0,30000,0` and **no other value** | Not refused: one copy, `copies` 1 id, `notCopied` 0, `copiedTo` empty; the reply's inputs line reads *toLevels not given - "", the card's own default (optional)*, and the same for `turnDegrees` and `turnCentreMm`. Version 5 refused it `needs_request_values` naming the three - if that comes back, this FAILS. **KEPT once, on Revit 2024 only** (`try-once` keeps a version once, D-119), then read with `REPORT_BOUNDING_BOX` beside the original: the copy's box 30000 mm further along y than the original's, the same in x and z. On Revit 2020 it is rolled back |
| ~~**PJ2**~~ | *Positive, the same copy typed blank.* The same roof, `offset=0,30000,0`, `toLevels=`, `turnDegrees=`, `turnCentreMm=` | The same answer as PJ1 - one copy, the same `copies` count, `copiedTo` empty - so leaving the three out and typing them blank are one call. The inputs line reads *given empty* for the three |
| ~~**PJ3**~~ | *Negative - a turn with no levels.* The roof, `offset=0,30000,0`, `turnDegrees=15`, `toLevels` left out | Refused as version 5 refused it - *"A turn is applied only when copying onto levels - name them in toLevels ..."*, ending *"Nothing was copied."*; `copies` 0 items; the roof count unchanged. Leaving `toLevels` out reads as blank, never as a level |
| ~~**PJ4**~~ | *Negative - a value given still does what it did.* The roof, `offset=0,0,0`, `toLevels=Level 99` | Refused naming *"Level 99"* and the model's levels, lowest first, *"Nothing was copied."* - the proof's own negative, unchanged |

**Releasing the hold, once PJ1 to PJ4 pass:** the owner removes `proof-held` and sets `heron-status` back
to PROVEN in the same change, or has `tools/batch-prove.py` write a new proof - never a session, and never
on a working model.

### Results, 2026-11-26 - Revit 2020, session 32484, add-in 0.3.0.0 (file `df0d401c52ca05f4`), card version 6 from main `02780d9a4`

**Arranged:** `pj-rac-basic` - a copy of Revit 2020's own `rac_basic_sample_project.rvt` in `%TEMP%\heron-work\pj`
(the installed sample untouched), not a new project with one roof. It has two roofs: **724430** (*Warm Roof -
Timber*) and **243274** (*SG Metal Panels roof*), which carries skylights. Every run rolled back; the roof carried by
`filter-elements-by-category` or `filter-elements-by-id` into `copy-elements --expect-from`. Proof job
[`pj-copy-elements-v6-...`](../../tools/jobs/pj-copy-elements-v6-revit2020-2026-11-26.yaml) - **PASS** (positive
PJ1, negative PJ4), a draft waiting for the owner; the card's `proof-held` is his to lift.

| Row | Result |
|---|---|
| PJ1, run 2026-11-26, Revit 2020 | **PASS on 2020** - roof 724430, `offset=0,30000,0` and nothing else: NOT refused `needs_request_values` - `copies` 1, `notCopied` 0, `copiedTo` empty. Both roofs together ran too, and Revit would not keep the copy - *"Can't cut instance(s) of 1180 x 1170mm out of its host"*, the skylights in roof 243274 - so Heron rolled it back: the binder took the call, Revit refused the model. **Still owed:** the KEPT run on Revit 2024 with `REPORT_BOUNDING_BOX`. The reply's inputs line was not printed by the command-line client, so its wording was not seen. |
| PJ2, run 2026-11-26 | **PASS** - the same roof with `toLevels=`, `turnDegrees=`, `turnCentreMm=` typed blank: `copies` 1, `notCopied` 0, `copiedTo` empty - the same answer as PJ1. |
| PJ3, run 2026-11-26 | **PASS** - both roofs, `turnDegrees=15`, `toLevels` left out: *"A turn is applied only when copying onto levels - name them in toLevels. ... Nothing was copied."*, `copies` 0. |
| PJ4, run 2026-11-26 | **PASS** - `offset=0,0,0`, `toLevels=Level 99`: *"This model has no level named "Level 99". Its levels, lowest first: Foundation, Level 1 Living Rm., Level 1, Ceiling, Level 2, Roof Line. Nothing was copied."* |
