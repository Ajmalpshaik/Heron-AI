# Needs checking — Group AG

> One group of [the register](../NEEDS-CHECKING.md), in its own file since 2026-09-23 so that it can be read
> alone. **The register's rules, and every group's place in it, are on that page.** A new row
> for this group goes in this file. [`tools/needs-checking-register.py`](../../tools/needs-checking-register.py)
> reads it back into the register for every tool that reads the register, so a row here is seen
> exactly as it was seen there. Written by
> [`tools/split-needs-checking.py`](../../tools/split-needs-checking.py).

## Group AG - the background Model Auditor, `heron-model-auditor` ([PR #298](https://github.com/Ajmalpshaik/Heron-AI/pull/298))

Heron's first background agent: `.claude/agents/heron-model-auditor.md`, which audits the open model with the read-only Heron tools its `tools:` line names - 18 when it ran, and three more since 2026-09-23, when package C2 added `revit_read`, `heron_lookup` and `heron_resolve` (Group AH). **Run once, not proven.** On 2026-09-22 it ran on **Project4**, Revit 2024, session 37184. Its report matched direct reads, including two areas read only after the run: views (12 views, 13 templates) and rooms (4, with 0 unplaced and 0 not enclosed). It declined a request to select all the walls. Its own list of callable tools showed only its 18 read tools. The model had 3,276 elements before and after. The evidence is in the PR.

*Placed 2026-09-23 by the register split, from the session that ran it. Its ids were first AC1 to AC3, which Stage 5's rows further down already used - AE and AF are taken as well, by the `heron-install` rows - so it was renumbered AG1 to AG3 the same day. FRAGMENT-ISSUES row 5b-156.*

| # | Check | Expected |
|---|---|---|
| **AG1** | In a new session, `audit my model with heron-model-auditor` on a **second** model that differs from Project4 (one with sheets and MEP systems) | The numbers move with the model: sheets, systems and annotation read differently from Project4 ([D-53](../DECISIONS.md) tracking). If it reports Project4's numbers again, it has read the wrong document |
| **AG2** | Run the Codex copy, `.codex/agents/heron-model-auditor.toml`, once | The same report shape. It must never call `revit_change`, `revit_apply_move`, `revit_preview_move`, `revit_select_by_category`, `revit_use_session` or `revit_use_this_model`. On Codex that is an instruction, not a restriction |
| **AG3** | Sign | A person reads both runs and signs. The machine never signs |

---
