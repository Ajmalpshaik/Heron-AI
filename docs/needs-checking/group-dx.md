# Needs checking — Group DX

> One group of [the register](../NEEDS-CHECKING.md), in its own file since 2026-10-13 so that it can be read
> alone. **The register's rules, and every group's place in it, are on that page.** A new row
> for this group goes in this file. [`tools/needs-checking-register.py`](../../tools/needs-checking-register.py)
> reads it back into the register for every tool that reads the register, so a row here is seen
> exactly as it was seen there.

## Group DX - copying a storey onto other levels: `COPY_ELEMENTS` version 2, Paste Aligned to Selected Levels with a turn per level (2026-10-14)

**What changed.** `COPY_ELEMENTS` (FRG-GEO-005) takes `toLevels`, `turnDegrees` and `turnCentreMm` as well as
its offset, and copies what it is given onto each level named - each copy belonging to its level at its
original's offset, each level's copy turned one more step about the plan point
([FRAGMENT-ISSUES 5b-460](../fragment-issues/section-5b-rows-401-425.md)). With the three blank it is version 1.
building-shell step 5 now draws a typical storey once and copies it.

**What is proved and what is not.** Run on Revit 2020 only, in a new unsaved Project2 (session 33484) on
2026-10-14: kept onto Levels 5 to 7 with a 2.8125 degree turn and read back through Heron's own reads, a kept
refusal for an unknown level, and a rolled-back `validate` positive and negative. The live MCP server read
`main`'s version 1, so version 2 ran through the bridge client from the branch -
`HERON_CLIENT_ID=ajmal-pc python mcp/client/heron_bridge_client.py fragment --in "Project2" copy-elements
--expect-from select-by-level --write --apply ...` - which is the same add-in path `revit_change` takes. Walls,
a curtain wall and a slab were copied; nothing else.

The proof draft and its run record are git-ignored, so they are not in this change: they sit in the worktree
that made them (`.claude/worktrees/compassionate-wiles-fe9927/brain/proof-drafts/`) and a copy in
`%TEMP%\heron-work\copy-levels`. The main checkout's `brain/proof-drafts/runs/copy-elements.json` is version 1's
September record - move it aside before copying these in to sign there, because `accept` reads the record from
that one place. Sign after the change is merged, so the card on `main` is version 2: `accept` refuses a draft
whose fingerprint is not the card's.

| # | Run | Look for |
|---|---|---|
| ~~**DX1**~~ | **DONE 2026-10-14** - signed by Ajmal PS on PR #526's branch, and promoted to PROVEN. ~~Read [`brain/proof-drafts/copy-elements.yaml`](../../brain/proof-drafts/README.md), then `python brain/heron_validate.py accept copy-elements --by "Ajmal PS"` and set `heron-status: PROVEN`~~ | positive: Level 8..Level 10, each *"5 of 5 belong to Level N, 5 at their original's offset, each exactly ... mm above its original; 4 of 4 plan point(s) where a turn of ... deg puts them; all 6 panel(s) ... on their host's level"*; negative: `toLevels=Level 99` refused, *"Nothing was copied"*, 0 copies; fingerprint `e99fc62f3cec4117` - **Struck 2026-10-18:** done 2026-10-14, as it says (fingerprint `e99fc62f3cec4117`). The card is DRAFT again at version 3 ([Group GP](group-gp.md)). |
| **DX2** | In Revit, Project2 (or any scratch copy of it): look at Levels 5, 6 and 7 in 3D, then a floor plan of each | each storey the Level 1 box turned one more 2.8125 degree step about (5000, 4000) mm, the glazing on the north side of each, nothing left at ground level - the eye check the read-backs cannot give |
| **DX3** | On a scratch model with a duct, a pipe, a ceiling, a roof and a level-based family on one level: `COPY_ELEMENTS` with `toLevels` naming two levels above | each kind's copy reads the level it was given at its original's offset - none of these kinds has been copied onto a level yet |
| **DX4** | Revit 2024 or later: the DX1 run | the same answer on a release where `ElementId` is 64-bit and the plan-to-plan copy has not been measured |
| **DX5** | The first use, on the owner's word: a new twisting tower - one storey drawn on Level 1, then `COPY_ELEMENTS` with `toLevels=Level 2..Roof`, `turnDegrees=2.8125`, `turnCentreMm=26000, 20000` | one call where the tower of 2026-10-13 took 69; every level's line says all its copies belong to it |
