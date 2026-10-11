# Needs checking — Group CH

> One group of [the register](../NEEDS-CHECKING.md), in its own file so that it can be read alone.
> **The register's rules, and every group's place in it, are on that page.** A new row for this group
> goes in this file. [`tools/needs-checking-register.py`](../../tools/needs-checking-register.py) reads
> it back into the register for every tool that reads the register.

## Group CH - PLACE_HOSTED_FAMILY version 4: a family into the roof, floor or ceiling at each point, and a window that faces out (2026-10-06)

**Built 2026-10-06 (PR #423) on rows [5b-325](../fragment-issues/section-5b-rows-301-325.md), [5b-326](../fragment-issues/section-5b-rows-326-350.md) and [5b-293](../fragment-issues/section-5b-rows-276-300.md).
[`place-hosted-family`](../../brain/fragments/place-hosted-family/fragment.yaml) is DRAFT, version 4, and
compiles on Revit 2020 to 2027.** It reads the family's own Host setting - Wall, Floor, Ceiling or Roof -
and places into the host found at each point; a window keeps facing its wall's exterior.

**Already measured, rolled back, in Project2** (Revit 2024, session 51820), a never-saved scratch
project the owner opened for it, arranged as
[`tools/jobs/place-hosted-family-v4-2026-10-06.yaml`](../../tools/jobs/place-hosted-family-v4-2026-10-06.yaml)
says: a skylight into the flat roof on Level 2 with Level 1 named, and nothing at a point with no
roof; two windows facing out on their walls, and nothing at points with no wall; a ceiling light in
the ceiling at its underside, turned 90 degrees; two skylights turned 90 and 30 degrees; a point under
two ceilings refused naming both; one already there found, not doubled, and turned; face-based and
level-based families refused naming the tool that places them. The run records are on the owner's PC,
git-ignored, in `brain/proof-drafts/runs/` of the worktree that ran them - `place-hosted-family.json`
is the skylight's, the one to sign.

| # | Run | Look for |
|---|---|---|
| **CH1** | **Sign the skylight proof** - read `brain/proof-drafts/place-hosted-family.yaml`, then `python brain/heron_validate.py accept place-hosted-family --by "Ajmal PS"`, in the worktree that holds it | Positive: placed 1 into roof 353520 'Generic - 400mm' on Level 2 although Level 1 was named; its Level reads 'Level 2'. Negative: placed empty, *"no roof on any level is over it"*. Fingerprint `98a6ae3870d0de99`, the code as committed. The gap it names - no second route - is CH4 |
| **CH2** | **Look at Project2 in Revit** - KEPT there on 2026-10-06: one M_Skylight-Flat 1200 x 1200mm in the roof at (4000, 3000), two M_Fixed 0915 x 1220mm at a 900 mm sill on the south wall at (4000, 0) and the east wall at (8000, 3000), one M_Troffer Light in the ceiling at (6000, 4000) turned 90 degrees | Select each: Properties names its host - the roof, the wall, the ceiling. A section through the skylight shows it cutting the roof, which read 22.243 m3 and 55.604 m2 against 22.704 m3 and 56.76 m2 uncut. Each window's exterior - its flip arrows' side - faces outside. Each placement is one Ctrl+Z |
| **CH3** | **The floor path** - a FLOOR-hosted family: one made from `Metric Generic Model floor based.rft`, or a manufacturer's - at points over a slab on its level, and one at a point off it | In that floor, at its top face, read back like the roof's. **Negative:** the point off the slab in `noHost`, naming the levels where a floor IS there. Not run: no floor-hosted family was in the library on this PC - every drain, outlet and register tried was face-based or level-based, and was refused |
| **CH4** | **The second route, by hand** - Revit's own Architecture > Window with M_Skylight-Flat, in a roof plan, clicked into the same roof beside one this placed | Both read the roof as their host, both cut the roof by the same opening - its Volume drops the same for each - and both move with the roof's edge. Compare their Level: this tool's reads the roof's own level |
| **CH5** | **A sloped roof** - a skylight over a pitched roof, rotation blank and then 90 | Hosted in the roof where the vertical line meets its sloped face. Whether a turn about the vertical line behaves on a slope is not measured |

**Nothing to put right in "Heron loads test":** the eight windows version 3 turned inside-out there
were turned back by hand with FLIP_ELEMENTS and read back facing out (row 5b-325). No add-in deploy
and no Revit restart are needed for version 4 - it is fragment code, read from disk on every call;
chats use it once the PC's main checkout has the merge.
