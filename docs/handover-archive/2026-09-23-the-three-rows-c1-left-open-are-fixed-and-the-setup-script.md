# Session note — THE THREE ROWS C1 LEFT OPEN ARE FIXED, AND THE SETUP SCRIPT HAS TO BE PASTED AGAIN

> **Archived session note** from 2026-09-23. It was moved out of [`HANDOVER.md`](../HANDOVER.md) on
> 2026-09-23 by [`tools/archive-handover.py`](../../tools/archive-handover.py), so that file could go back to
> being the short live entry point it is supposed to be. **Its words are unchanged; only its links were
> re-pointed.** Nothing here is specification: where it disagrees with [DECISIONS.md](../DECISIONS.md),
> the [Golden Rules](../14-golden-rules.md) or the [Constitution](../../HERON_CONSTITUTION.md), **those
> win**. A note records what was true on its own day.

---

### 2026-09-23 — THE THREE ROWS C1 LEFT OPEN ARE FIXED, AND THE SETUP SCRIPT HAS TO BE PASTED AGAIN

A cloud session, no Revit, the same day as C1: FRAGMENT-ISSUES rows 5b-167 to 5b-169, fixed on the
owner's word in [PR #316](https://github.com/Ajmalpshaik/Heron-AI/pull/316). All of it is for
developing Heron; none of it reaches a model or a modeller.

- **Fixed:** [`tools/cloud-setup.sh`](../../tools/cloud-setup.sh) installs past a refused `apt-get update`
  and past Debian's own Python packages, and its last lines name whatever did not arrive (row 5b-167).
  **It reaches no session until the owner pastes it into the environment's Setup script box -
  NEEDS-CHECKING Group AK.** The `heron` MCP server still will not connect in a cloud session: it exits
  off Windows by design (row 5b-162, open).
- **Fixed:** `heron_bridge_client` names the machine-local folder .NET names, on every system, and never
  a relative one (row 5b-168). On Linux the hooks' diary is in `~/.local/share/Heron/logs` now.
- **Fixed:** the three pages that said Heron has no hooks (row 5b-169).
- **Found, recorded, not fixed:** five Python files resolve a Windows special folder outside the two path
  owners and the structure gate cannot see them (row 5b-182); `PROJECT-MAP.md` says CI decides on ten
  gates, and it is eleven (row 5b-183).
- **Mistake worth not repeating:** register ids were taken twice more while this was being written.
  Read the section's highest id immediately before adding a row, never the one seen an hour earlier -
  row 5b-163 names the pattern.
