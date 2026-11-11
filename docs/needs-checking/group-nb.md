# Needs checking — Group NB

> One group of [the register](../NEEDS-CHECKING.md), in its own file so that it can be read alone. **The
> register's rules, and every group's place in it, are on that page.** A new row for this group goes in
> this file. [`tools/needs-checking-register.py`](../../tools/needs-checking-register.py) reads it back
> into the register for every tool that reads it, so a row here is seen exactly as it was seen there.

## Group NB - a nested DETAIL ITEM locked to named planes in the plan it is drawn in: `LOCK_NESTED_FAMILY_TO_PLANES` version 3, now version 4 (2026-11-11)

On 2026-11-11 (Revit 2020) four nested `Diffuser Flow Arrow_DRAFT` detail items in the owner's square diffuser
family, placed with `PLACE_FAMILY_INSTANCES` on "Ref. Level" and turned with `ROTATE_ELEMENTS`, refused every
lock with "The nested family has no extent to read back" - Revit gives a view-based instance no box outside a
view - and the owner locked them by hand with Align. The card was widened, so it is DRAFT again at version 3;
why, and what is not known yet, is [FRAGMENT-ISSUES row 5b-1492](../fragment-issues/section-5b-rows-401-425.md).

**Version 4, the same day, after NB1 failed.** NB1's positive (Revit 2020, session 37656, the scratch family below)
threw the same "The nested family has no extent to read back": version 3 knew a detail item only by its owner
view, and a detail item placed with `PLACE_FAMILY_INSTANCES` on "Ref. Level" - the only way Heron places one in a
family - goes down on the level's plane and has NO owner view. Version 4 also knows a detail item by its category
(Detail Items) or its family's View Based placement, and locks one with no owner view in the floor plan of the
level it sits on (an elevation for one on a vertical plane), saying which in a finding. Everything below now
tests **version 4**; why, and what is still not known, is
[row 5b-1494](../fragment-issues/section-5b-rows-401-425.md).

**`--negative-set` REPLACES the values, it does not add to them.** Repeat `instance=` in it, or the negative leg
stops at `needs_request_values` and never runs - which is what happened to NB1's first negative.

| What | Where | What it does |
|---|---|---|
| `LOCK_NESTED_FAMILY_TO_PLANES` version 4 (MODIFY) | [`brain/fragments/lock-nested-family-to-planes`](../../brain/fragments/lock-nested-family-to-planes/fragment.yaml) | A nested family whose `OwnerViewId` names a view - a detail item - is locked IN that view (not the first floor plan or elevation, as a model family is), and only to a plane seen edge-on there; it is read back by its insertion point (`GetTransform().Origin`) and, where Revit gives one, its box in that view. Version 4: a detail item with NO owner view (known by its category Detail Items or its View Based family) is locked in the floor plan of its level, or an elevation for one on a vertical plane. A model family takes exactly the path of version 2 |

**What is done and what is not.** The card compiles on all eight releases, 2020 to 2027, with the whole library
(`tools/check-fragments-compile.py`, 2026-11-11, the owner's PC). Every new Revit member is in the card's
compatibility note, looked up with `tools/api-surface --members`. **Nothing here has run in Revit.**

**The model - a SAVED scratch family, never the owner's open families.** File, New, Family, the metric Generic
Model template, saved as "Heron Detail Lock Test" under `%TEMP%\heron-work\detail-lock`, Revit 2020; and a
detail item from the metric Detail Item template - a 600 mm arrow, its tail at the origin pointing +X - saved
beside it and loaded. Every line with `HERON_CLIENT_ID=ajmal-pc`, `--session <process id>` when two Revits are
connected, and `< /dev/null`.

| # | Run | Look for |
|---|---|---|
| **NB0** | *Open and arrange, KEEP it.* In the scratch family: planes "Arrow Front" and "Arrow Back" (facing front-back, 390 mm either side of Center (Front/Back)), labelled EQ with a length "Arrow Span" (780); the arrow placed in "Ref. Level" with `PLACE_FAMILY_INSTANCES`, turned 90 degrees with `ROTATE_ELEMENTS`, moved with its tail on "Arrow Front" and its line on Center (Left/Right). Record its id. Save | The title bar names the scratch family; the arrow, selected, shows no padlock |
| **NB1** | *The D-30 negative FIRST, then the positive.* Rolled back. Negative: `--negative-set "instance=<id>" --negative-set "locks=Center (Front/Back)=Ref. Level"` - `instance` repeated. Positive: `locks=Center (Front/Back)=Center (Left/Right)`. **First run 2026-11-11 on version 3: the positive threw "no extent to read back", the negative did not run (row 5b-1494)** | Negative: refused before anything is locked - the side faces left-right, the level up-down - and **NOT** "no extent to read back". Positive: a finding that the detail item belongs to no view and is locked in "Ref. Level"; one lock, "(in Ref. Level)", the insertion point unmoved - **if Revit refuses to align a detail item's reference in its owner view through the API, that is the first thing this group learns** and the card is changed, not proved |
| **NB2** | *A plane it does not lie on.* Rolled back: `instance=<id>`, `locks=Center (Front/Back)=Arrow Back` | Revit's own refusal named with "move the nested family onto the plane first"; the call rolled back, nothing locked |
| **NB3** | *FOLLOW, kept.* NB1's positive and a second lock on the arrow's own plane at its tail (`Left`, if the arrow family gives its tail plane Is Reference Left - else run `SET_FAMILY_PLANE_REFERENCE` in it first) to "Arrow Front", through `try-once` (or `revit_change` once PROVEN) and KEEP; `FLEX_FAMILY` "Arrow Span" at 600 and 1,200 | The arrow's insertion point read back 300 and 600 mm from Center (Front/Back) - it followed "Arrow Front"; selected, two closed padlocks |
| **NB4** | *A model family, unchanged.* Version 2's positive on a nested 3D box (tests/cases.yaml, the first positive), rolled back | Two sides locked in the floor plan, its extent unchanged - the path a model family took before version 3 |
| **NB5** | *Second route.* By hand on a copy: Align in "Ref. Level", the plane, the arrow's plane, the padlock; flexed side by side with NB3 | The two copies read back the same at every size |

**Before the owner's own family.** The first run on `KDF Square Diffuser - Supply - Detailed_DRAFT.rfa` is his
choice, through `try-once`, behind Revit's Keep or Discard. Its four arrows were locked BY HAND on 2026-11-11:
locking them again through Heron makes a second alignment beside each, which is not measured here - release the
hand locks first, or run it only on arrows placed afresh.

**Run 2026-11-11, ~13:00-13:05 local - version 4, NB1, NB2 and NB3 PASS** (Revit 2020, session 44624, the scratch family
"Heron Detail Lock Test", 1,529 elements; arrow id 3036 / `931ee163-891c-4259-9427-fb96141d1c74-00000bdc` at (0, -390),
turned 90). NB1 through `validate --write`: negative `locks=Center (Front/Back)=Ref. Level` refused before anything was
locked - "faces left-right where it stands, and Ref. Level faces up-down" - and NOT "no extent to read back"; positive:
"a detail item that belongs to no view ... locked in Ref. Level", one lock, Center (Front/Back) to "Center (Left/Right)".
NB2 (`locks=Center (Left/Right)=Arrow Back`, a parallel plane 780 mm off): Revit's own "not geometrically aligned",
named with "move the nested family onto the plane first", rolled back. NB3 through `try-once`, KEPT by the owner: two
locks - Center (Front/Back) to "Center (Left/Right)", Center (Left/Right) to "Arrow Front" (record
`brain/proof-drafts/runs/lock-nested-family-to-planes.once.json`, local); "Arrow Span" 600 read the arrow at (0, -300),
1,200 at (0, -600) - it followed; set back to 780. `validate --write --kept` then ran the negative again (refused, as
NB1). **The control, on the owner's diffuser the same day:** its four nested arrows, placed by PLACE_FAMILY_INSTANCES
and never locked, stayed at +-476 mm when 150x150 moved the "Arrow" planes to +-183.3 - a nested detail item does not
follow a plane by itself, so the lock is what moved the scratch arrow. **Not run:** NB4 (a 3D nested family on version 4),
NB5 (the hand route), any release but 2020.

**SIGNED 2026-11-11 by Ajmal PS - `LOCK_NESTED_FAMILY_TO_PLANES` version 4 is PROVEN** on NB3's kept run with NB1's
negative (the proof block in the card). Still open, not blocking: NB4, NB5.
