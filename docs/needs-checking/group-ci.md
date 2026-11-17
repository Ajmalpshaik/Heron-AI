# Needs checking — Group CI

> One group of [the register](../NEEDS-CHECKING.md), in its own file so that it can be read alone.
> **The register's rules, and every group's place in it, are on that page.** A new row for this group
> goes in this file. [`tools/needs-checking-register.py`](../../tools/needs-checking-register.py) reads
> it back into the register for every tool that reads the register.

## Group CI - family type values: a material by its name and a URL as text (2026-10-06)

**Tracked as GitHub issue [#980](https://github.com/Ajmalpshaik/Heron-AI/issues/980) since 2026-11-16** - the checks still owed are listed there; tick the group in that issue when they are done. Every result stays in this file.

`SET_FAMILY_TYPE_VALUES` version 2 writes a **material** parameter by the material's name and a **URL**
(and any parameter Revit keeps as text) as text. Version 1 refused both - seen 2026-10-06 in the family
`GM_PipeSupport_HexNut` with `Nut Material=TRG_Steel_Galvanised` and `URL=https://ajmalps.com`, which then
had to be typed in the Family Types dialog ([row 5b-341](../fragment-issues/section-5b-rows-326-350.md)).
The change is under `impl/`, so the version 1 proof went stale; **signed by Ajmal PS on 2026-10-06** on the CI1/CI2 record below, and PROVEN again.

**Run on 2026-10-06, rolled back**, on the scratch family *Family3* (Revit 2024, unsaved, made with
`CREATE_FAMILY_DOCUMENT` from *Metric Generic Model.rft*), never on the owner's `GM_PipeSupport_*` files.
The setup added a type material parameter `Nut Material` with `ADD_FAMILY_PARAMETERS` inside the same
rolled-back group; `URL` is the family's own built-in parameter.

| # | Run | Look for |
|---|---|---|
| ~~**CI1**~~ | validate --in "Family3" --negative-in "Family3" --write --setup add-family-parameters… — [full row](../needs-checking-archive/group-ci.md#row-ci1) | **RAN 2026-10-06:** |
| ~~**CI2**~~ | the same, `values=Nut Material=Unobtainium; URL=https://ajmalps.com` — [full row](../needs-checking-archive/group-ci.md#row-ci2) | **RAN 2026-10-06:** |
| ~~**CI3**~~ | `values=Nut Material=<By Category>` — [full row](../needs-checking-archive/group-ci.md#row-ci3) | **RAN 2026-10-06:** |
| ~~**CI4**~~ | the signature — [full row](../needs-checking-archive/group-ci.md#row-ci4) | **DONE 2026-10-06:** |
| **CI5** | a material name that two materials share in different cases | refused naming both, nothing written. **Not run** - a family holding both was not arranged |
| **CI6** | CI1 on **Revit 2020** and **2027** | The same answers. Compiled for 2020 to 2027; `ParameterType.Material` / `URL` before 2022 is read by reflection and has not run there |

**A value that holds a semicolon cannot be typed** - the values are split on `;` before the fragment sees
them. A URL with a `;` in it is the one case likely to meet this; none of the owner's do.
