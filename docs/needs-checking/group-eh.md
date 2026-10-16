# Needs checking — Group EH

> One group of [the register](../NEEDS-CHECKING.md), in its own file since 2026-10-15 so that it can be read
> alone. **The register's rules, and every group's place in it, are on that page.** A new row
> for this group goes in this file. [`tools/needs-checking-register.py`](../../tools/needs-checking-register.py)
> reads it back into the register for every tool that reads the register, so a row here is seen
> exactly as it was seen there.

## Group EH - a reference inside a loaded link: `CREATE_LINEAR_DIMENSION` version 4 (2026-10-15)

Asked for on 2026-10-15 on a real project (Revit 2020, a BIM 360 model that is never written to for a proof):
in four builders-work plans the owner had tagged ten Generic Model cut-outs that live in a LINKED model, and
asked for dimension chains from a grid to both sides of each. Every word
[`CREATE_LINEAR_DIMENSION`](../../brain/fragments/create-linear-dimension/fragment.yaml) (FRG-VIEW-101)
version 3 takes looks only in the host, so none of the cut-outs could be named.
[Row 5b-486](../fragment-issues/section-5b-rows-401-425.md) has the finding.

**What changed.** Version 4 takes one word more in `between`, in front of any other: `link LINK WORD ...` -
LINK the placed link's id, its unique id or the linked model's name with or without `.rvt` - and the word after
it (grid, level, plane, line, wall, family or face) is read exactly as version 3 reads it, but inside the link's
own document; directions are carried into the host by the link's placement, so `face ID left` still means left
of the host's view. The reference is carried across with `Reference.CreateLinkReference` and rewritten into the
form Revit was measured to dimension to (EH0). Every version 3 word reads as it did. The card is **DRAFT**.

**Compiled for every release 2020 to 2027; run in Revit 2020 only, and not as a proof.** The members it calls
were read off the reference assemblies for all eight releases on 2026-10-15. Whether 2021 to 2027 hand back the
same written form from `CreateLinkReference` is not known: the rewrite only touches the form measured on 2020
and uses any other as it comes, so a release that differs is a run to make, not a guess already made.

**THE TEST PROJECTS ARE NEW ONES, NEVER THE REAL PROJECT.** The header of
[`tools/jobs/create-linear-dimension-link-2026-10-15.yaml`](../../tools/jobs/create-linear-dimension-link-2026-10-15.yaml)
says how to make them: a saved link source holding a 200 mm wall, Group EF's box rotated into it, a grid, a
reference plane and a model line, linked origin to origin into a new unsaved host with one grid and a section.
**Ask the owner before opening them** in a Revit that has his real project open, and have him Synchronize with
Central first.

**EH0 - MEASURED 2026-10-15, NOT THE PROOF.** On the owner's word, five probes ran on the READ path in his Revit
2020 (session 3460) while the real project was open: each made a box family, a link source (saved under
`heron-work`) and a host linking it, all with no window; drew every dimension inside a transaction it rolled
back; and closed all three documents. The real project was handed no transaction. What they found decided the
code:

| Asked | As `CreateLinkReference` hands it back | Rewritten `<link>:0:RVTLINK:...` | And `:0:SURFACE` after a whole element |
|---|---|---|---|
| a linked box's face, symbol geometry, from a host grid | refused - "not geometric references" | **2000 mm**, drawn 2000 | - |
| the same face, instance geometry | refused - "invalid number of references" | 2000 mm, but reads back as the family TYPE, not the placed box | - |
| the box's CenterFrontBack plane | refused - "not geometric references" | **2500 mm**, drawn 2500 | - |
| a linked wall's two faces | refused - "invalid number of references" | **200 mm**, the wall's thickness | - |
| a linked model line | - | **1500 mm**, drawn 1500 | - |
| a linked grid, as the element | refused | refused - "invalid number of references" | **500 mm**, drawn 500 |
| a linked level, as its plane | - | refused | **1600 mm** to the box's top, drawn 1600 |
| a linked reference plane | - | refused | **1000 mm**, drawn 1000 |

`:1:...` instead of `:0:...` measured -304.8 mm - nonsense - so the index is not free. The same held in a plan
with a view template, one without, and a section. Then the fragment itself, this branch's code, ran 33 cases on
the same scratch projects, the last run after the final change: every positive read what was built - among them
grid C to the linked box's two faces `2000 mm + 1000 mm`, a host grid through a linked grid, plane and line to the
box `500 + 500 + 500 + 500 + 1000`, the linked level to the box's top `1600`, the box's sides in a section
`300 + 2350`, the link rotated 90 degrees `1000 + 2000` with left and right OF THE VIEW, the link by its name and
by `"HeronLinkSource.rvt"`, and version 2's `grid C | grid D` unchanged at `5000` - each reading back its linked
references as linked elements. Every negative was refused in plain words with nothing drawn: a link the host does
not place (naming the ones it does), an id not in the link, a linked id said without `link` (naming the link it is
in), a link inside a link, a link with no word after it, a side that is not one of the six, a family reference it
does not publish, a grid and a plane the link does not have, the link hidden in the view, and the link unloaded.
Revit posted no warning in any run. **What it does not settle:** no proof draft, no signature, scratch projects
made by the API rather than by hand, one release, and no second route.

| # | Run | Look for |
|---|---|---|
| **EH1** | The job file's first job: `grid C \| link LINKID family BOXID CenterLeftRight`; `... family BOXID Left` as the negative | measured `2500 mm`; the negative refused naming CenterLeftRight and CenterFrontBack and offering `link LINKID face BOXID ...`; nothing created |
| **EH2** | The second: a linked grid, reference plane and model line, the link by its NAME; `grid C \| link Nope grid LC` as the negative | measured `500 mm + 500 mm + 500 mm`; the negative refused naming `HeronLinkSource.rvt` and its id; nothing created |
| **EH3** | The third: the linked wall's exterior and interior; `grid C \| face BOXID bottom` as the negative | measured the wall's thickness; the negative refused saying the box's id is in link HeronLinkSource, with `"link LINKID ..."`; nothing created |
| **EH4** | The fourth, in "Heron Link Section": `link LINKID level Level 1 \| link LINKID face BOXID top`, one point twice; a level the link does not have as the negative | measured `1600 mm` and "The string runs through 2000,-1000,800 mm"; the negative refused by name; nothing created |
| **EH5** | The fifth, which writes the proof draft: `grid C \| link LINKID face BOXID bottom \| link LINKID face BOXID top`; `... face 99999999 left` as the negative | measured `2000 mm + 1000 mm`, and "2 of its references ... read back ... as an element of a linked model"; the negative refused naming the link; nothing created |
| **EH6** | The second route: in the host plan, Revit's own Annotate > Aligned dimension, Tab into the link, picked on grid C and the box's two faces, by hand | the same values as EH5 |
| **EH7** | Version 3's own cases still hold - [Group EF](group-ef.md) EF1 to EF5 - since every version 3 word must read as it did | the values Group EF lists |
