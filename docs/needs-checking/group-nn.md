# Needs checking — Group NN

> One group of [the register](../NEEDS-CHECKING.md), in its own file so that it can be read alone. **The
> register's rules, and every group's place in it, are on that page.** A new row for this group goes in
> this file. [`tools/needs-checking-register.py`](../../tools/needs-checking-register.py) reads it back
> into the register for every tool that reads it, so a row here is seen exactly as it was seen there.

## Group NN - a reference plane at an angle: `CREATE_REFERENCE_PLANES` version 2 (2026-11-12)

Built 2026-11-12 while proving `LABEL_FAMILY_DIMENSION` v3 (Group NE): its angle rows need two sloped planes, no Heron
tool drew one, and the owner asked that the missing tool be built and proved in the same scratch family. Widened,
not made new - the same job, a named reference plane. Why, and what is not known, is
[FRAGMENT-ISSUES row 5b-1564](../fragment-issues/section-5b-rows-401-425.md).

| What | Where | What it does |
|---|---|---|
| `CREATE_REFERENCE_PLANES` version 2 (MODIFY) | [`brain/fragments/create-reference-planes`](../../brain/fragments/create-reference-planes/fragment.yaml) | axis "angle": each plane Name=degrees, a vertical plane in the floor plan through `throughMm` ("X,Y" mm, the origin when left out), read back by its normal and the point. Version 1's axes and distances unchanged |

**Compiled** on Revit 2020 and 2027 with the whole library (`tools/check-fragments-compile.py`, 2026-11-12, the
owner's PC).

**The model:** the SAVED scratch family `%TEMP%\heron-work\link-angle\Heron Link Angle Test.rfa`, metric Generic
Model, Revit 2020, session 33000 - never the owner's own families.

| # | Run | Look for | Result 2026-11-12 |
|---|---|---|---|
| **NN1** | *Trial, rolled back* - axis angle, planes "Arrow Slope=30; Arrow Low=-15" | created 2, each at its angle through 0,0 mm | **held** |
| **NN2** | *The positive, KEPT* - the same through `try-once` | the two planes stay; the owner presses Keep | **KEPT by Ajmal PS**: "Arrow Slope (at 30 degrees from left-right, through 0,0 mm), Arrow Low (at -15 degrees ...)" |
| **NN3** | *The D-30 negative* - `validate --write --kept`, "Arrow Slope=45", axis angle | refused - already a plane at 30 degrees; nothing made, nothing turned | **held**: "already a reference plane in this family, at 30 degrees" |
| **NN4** | *Used* - Group NE's NE5, NE5b and NE6 label angles between these planes | 30, 45 and 150 degrees read back | **held** (NE6 after row 5b-1565's fix) |
| **NN5** | *Version 1 unchanged* - axis left-right, "Box Far=900" | created 1 at X 900 | NOT RUN on version 2 - Box Left / Box Right were made by version 1 earlier the same session |
| **NN6** | *Refusals* - throughMm "300,0" with axis left-right; throughMm "here" with axis angle | both refused, nothing made | NOT RUN |

**Proof draft:** `brain/proof-drafts/create-reference-planes.yaml` - NN2 kept and NN3 - awaits the owner's signature.
