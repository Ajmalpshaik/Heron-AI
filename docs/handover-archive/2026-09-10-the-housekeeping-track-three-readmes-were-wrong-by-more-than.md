# HANDOVER — 2026-09-10 (the HOUSEKEEPING track): three READMEs were wrong by more than a hundred, and the write path was documented as not existing

> **Archived session note.** This is a record of work that is finished. It was moved out of
> [`HANDOVER.md`](../HANDOVER.md) on 2026-09-12 so that file could go back to being the short
> live entry point it is supposed to be. Nothing here is specification: where it disagrees with
> [DECISIONS.md](../DECISIONS.md), the [Golden Rules](../14-golden-rules.md) or the
> [Constitution](../../HERON_CONSTITUTION.md), **those win**.

---


**Documentation only. No executable code was modified** — `git diff --diff-filter=M` over `.py`, `.cs`,
`.csproj`, `.props` and `.ps1` across the whole batch returns nothing. Its full ledger was a work note and was
**retired on 2026-09-12** once its last open item moved to a permanent register — the cold read is now
[NEEDS-CHECKING](../NEEDS-CHECKING.md) **R3**, and the Windows-only suite failure it found is **A14**. The
sections below are what survived it; the ledger itself is in git history, which is recovery evidence
rather than something to go looking in.

**The three findings worth knowing about:**

1. **The write path was documented as not existing.** The root `README.md` and `brain/README.md` both
   said running a fragment that WRITES *"does not exist"*. It does — `run_fragment_write`, registered
   `MODIFY`, dispatched in two files, governed by [D-55](../DECISIONS.md) — and **55 `MODIFY` fragments
   carry a recorded proof**, so it has met a real model. Corrected in both.
2. **Three READMEs carried a fragment count wrong by more than a hundred.** `brain/README.md` said
   *52 `PROVEN`, 308 `DRAFT`*; the truth was **167 and 193**. The root `README.md` and `docs/README.md`
   said 159/201. Every one of them already carried a sentence admitting the number goes stale —
   **writing that sentence is not enough**, and each now names the deriving command instead.
3. **The skills lived in three places and one of them never existed.** `.claude/skills/` and
   `.agents/skills/` held the same six skills, five byte-identical, while `.codex/agents/*.toml` told
   Codex to read `.Codex/skills/` — **a directory that has never existed**, so Codex was broken
   independently of either. On the owner's decision the tree is now one: `.claude/skills/`, matching
   [D-01](../DECISIONS.md), with `.codex` repointed at it and the `.agents` copy deleted after
   file-by-file verification that nothing unique was lost.

| | |
|---|---|
| **Created** | `AGENTS.md` · [`docs/PROJECT-MAP.md`](../PROJECT-MAP.md) · [`docs/work-notes/README.md`](../work-notes/README.md) · [`tests/README.md`](../../tests/README.md) |
| **Deleted** | `.agents/skills/` — 7 files, a duplicate |
| **Moved** | **Nothing.** Four candidate moves were assessed and all rejected with reasons. This file stays put because saved continuation prompts point at this exact path from outside the repository; it is **labelled** as the operational entry point instead |
| **Links** | **4 broken → 0.** All four were wrong filenames; each right target was found by reading content |
| **Also corrected** | The four specification parts disagreed about how many parts exist (1 of 2, 2 of 2, 3 of 3) · `CONTRIBUTING.md` said Phase 2 had never loaded into Revit · the `heron-ship` skill said *"35 of 38 pass"* when there are 41 suites, so its own *"a fourth failure is yours"* rule would have misled an agent |
| **Checks passing** | `check-docs` (0 broken links), `check-metadata`, `check-structure`, `check-licence`, `check-routing`, `check-intrusion`, `agent-count`, `heron_fragment`, `git diff --check`. `check-package`. `check-gaps` exits 1 on a plain container — its exit code follows the UNFINISHED list, whose only entry is `test_served_claims.py`, failing for want of the MCP SDK. Every fragment below PROVEN is in the *waiting* bucket. Run it for the counts; do not read one here |
| **Could not run** | The three compile gates and `test_bridge_roundtrip` — **no .NET SDK.** `test_mcp_serves` and `test_served_claims` — **no MCP SDK.** None counted as a pass |
| **Needs real Revit** | Everything in [NEEDS-CHECKING.md](../NEEDS-CHECKING.md). Unchanged by this run |

**Two things this run could not establish, and does not claim.** The drive-letter condition — repository
on one drive, `TEMP` on another — cannot be exercised where they share a filesystem, so the
`check-licence.py` repair is **still not re-proved** and wants one run on the owner's machine. And the
cold-read walkthrough is not claimed: the session that wrote the entry documents cannot cold-read them.

**Left alone deliberately:** `test_graph` (3 checks) and `test_reachable` (1 check) fail and are
pre-existing and unrelated — recorded, not fixed. So are the dated snapshots inside `DECISIONS.md`,
`FRAGMENT-ISSUES.md` and this file's own session records; those are labelled history.

---
