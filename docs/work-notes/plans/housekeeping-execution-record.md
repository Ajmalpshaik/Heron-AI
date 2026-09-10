# Housekeeping execution record

> **Type:** Operational work note — the durable ledger for the repository housekeeping
> run driven by [`repository-housekeeping-and-ai-onboarding-plan.md`](repository-housekeeping-and-ai-onboarding-plan.md).
> **Status:** Phase A complete. Nothing has been moved, merged or deleted; classification only.
> **Owner of this run:** single-agent session on branch `claude/amazing-fermat-emyav7`.
> Independent review is **pending** — self-review is not independent approval (plan §28.2).

This file exists because plan §28.11 item B requires the baseline suite result to be a
committed artefact, and §28.1 requires a single working ledger. Phase I compares against
**this file**, not against prose or recollection.

---

## 1. Phase status — what is done and what is not

| Phase | Title | Status | Remaining |
|---|---|---|---|
| **A entry gate** | Prove every gate runs (§28.11 A) | **DONE** | — |
| **A** | Repository-wide audit and inventory | **DONE** | Inventory built, all 82 Markdown files classified, conflicts recorded — §9 to §12 below |
| **B** | Truth and status reconciliation | **NOT DONE** | Root `README.md` line 45 known stale; not yet corrected |
| **C** | Documentation architecture | **NOT DONE** | — |
| **D** | Work-notes separation | **NOT DONE** | `docs/work-notes/README.md` does not exist |
| **E** | Module-level onboarding READMEs | **NOT DONE** | — |
| **F** | AI-first / human-first navigation | **NOT DONE** | — |
| **G** | Cross-link and stale-reference cleanup | **NOT DONE** | 4 pre-existing broken links stand (§5 below) |
| **H** | Permanent documentation quality | **NOT DONE** | — |
| **I** | Validation and QA | **NOT DONE** | Will diff against §4 of this file |
| **J** | Final repository audit | **NOT DONE** | — |
| **K** | Final handover report | **NOT DONE** | — |
| **L** | Plan self-removal | **BLOCKED** | Precondition unmet: `AGENTS.md`, `docs/PROJECT-MAP.md` and `docs/work-notes/README.md` do not exist (§28.11 F) |

---

## 2. Baseline and environment

| Field | Value |
|---|---|
| Date | 2026-09-10, 15:45 UTC |
| Baseline commit | `5aa7f6d5c3d962e64fdb4e00b8459ad3dede0c4e` |
| Branch | `claude/amazing-fermat-emyav7` |
| Remote | `https://github.com/Ajmalpshaik/Heron-AI` |
| Working tree at baseline | Clean — 0 modified, 0 untracked |
| Tracked files | 1329 |
| Markdown files | 81 |
| Python | 3.11.15 |
| Git | 2.43.0 |
| `.NET SDK` | **absent** |
| MCP SDK | **absent** |
| `numpy` | absent |
| `PyYAML` | present |
| `HERON_CLIENT_ID` | set per session, one id for this run |
| `HERON_KNOWLEDGE` | set to a scratch folder outside the repository |

### Environment limitation that changes how this record must be read

This run executes on **Linux**, at `/home/user/Heron-AI`, with the temporary directory on
the **same filesystem** as the repository.

Plan §28.11 was measured on Windows, at `D:\Ajmal\Aj Programs\Heron Ai`, with `TEMP` on `C:`.
§28.11 item C makes a differing drive letter a required walkthrough condition, because that
condition is exactly what exposed the `check-licence.py` defect.

**That condition cannot be exercised here.** `tests/test_licence_check.py` passing in this
environment does not re-prove the `check-licence.py` repair for the owner's setup; it only
shows the tool is not broken in the trivial same-filesystem case. The repair still needs one
run on the owner's machine to be confirmed by this run's evidence.

Every evidence entry below therefore carries this environment, not the plan's.

---

## 3. Phase A entry gate — §28.6 matrix, run once each

Required by plan §28.11 item A: exit code **and** wall-clock duration for every command,
before any file is classified. All timings from the baseline commit.

| Command | Exit | Duration | Verdict | Relevance |
|---|---|---|---|---|
| `git diff --check` | 0 | 0.0s | **PASS** | required |
| `python tools/check-docs.py` | 0 | 1.5s | **PASS** — 4 pre-existing broken links reported, non-fatal by design | required |
| `python tools/check-metadata.py` | 0 | 0.1s | **PASS** | required |
| `python tools/check-structure.py` | 0 | 0.1s | **PASS** | required |
| `python tools/check-licence.py` | 0 | 0.4s | **PASS** — see drive-letter limitation in §2 | required |
| `python tools/agent-count.py` | 0 | 0.1s | **PASS** — 250 agents, 71 built, 4 host-provided, 175 left; register reconciles | required |
| `python brain/heron_fragment.py` | 0 | 2.2s | **PASS** — 360 fragments, 360 well-formed | required |
| `python tools/check-reachable.py` | 0 | 0.5s | **PASS** — report, not a gate; findings are questions | required |
| `python tools/check-revit-gate.py` | 0 | 1.8s | **PASS** — report, not a gate | required |
| `python tools/check-routing.py` | 0 | 30.1s | **PASS** — needs `HERON_KNOWLEDGE`; see note below | required |
| `python tools/check-intrusion.py` | 0 | 20.8s | **PASS** — needs `HERON_KNOWLEDGE`; see note below | required |
| `python tools/check-gaps.py` | 1 | 236.3s | **PASS as a gate** — exit 1 is correct and expected; see §5 | required |
| 41 × `tests/test_*.py` | mixed | 226.7s total | see §4 | required |
| `python tools/check-compile.py` | 1 | 0.0s | **NOT RUN — no .NET SDK on this machine** | required, blocked |
| `python tools/check-fragments-compile.py` | 1 | 1.7s | **NOT RUN — no .NET SDK on this machine** | required, blocked |
| `python tools/check-api-surface.py` | 1 | 0.1s | **NOT RUN — no .NET SDK on this machine** | optional |
| Real Revit proof workflow | — | — | **NEEDS REAL REVIT** | not applicable to a Markdown-only batch |

### Knowledge-store dependency, recorded as §28.6 requires

`check-routing.py` and `check-intrusion.py` both aborted with
`ValueError: No %APPDATA% and no HERON_KNOWLEDGE`. This is an **environment
configuration, not a defect**: `%APPDATA%` does not exist off Windows. Both pass once
`HERON_KNOWLEDGE` points at a folder. Recorded here so a later run does not
re-diagnose it as a broken gate.

### Blocked gates and their owners

| Blocked gate | Reason | Owner | Consequence |
|---|---|---|---|
| `check-compile.py` | No .NET SDK in this container | Owner / a machine with the SDK | No C# claim may be made by this run |
| `check-fragments-compile.py` | No .NET SDK in this container | Owner / a machine with the SDK | No fragment-compile claim may be made |
| `check-api-surface.py` | No .NET SDK in this container | Owner / a machine with the SDK | Optional; not required by a Markdown batch |
| `tests/test_bridge_roundtrip.py` | Test host not built (needs `dotnet build`) | Owner / a machine with the SDK | Excluded from `check-gaps.py` **by design** |
| `tests/test_mcp_serves.py` | MCP SDK not installed | Owner, or `pip install --user mcp` | Self-reports SKIPPED, exits 3 on purpose |
| `tests/test_served_claims.py` | MCP SDK not installed | Owner, or `pip install --user mcp` | `check-gaps.py` counts it UNFINISHED, not WAITING |
| §28.11 C drive-letter condition | Repo and `TEMP` share a filesystem here | Owner's Windows checkout | `check-licence.py` repair not re-proved here |

Phase I may not treat any row above as a pass, and may not drop it from the matrix.

---

## 4. Baseline suite result — the artefact Phase I diffs against

41 suites on disk, each run once, serially, with a 300s per-suite bound — the same bound
`check-gaps.py` uses. Total 226.7s. Exit code 0 is a pass; 3 is a deliberate self-skip.

| Suite | Exit | Duration | Reading |
|---|---|---|---|
| `test_backup.py` | 0 | 0.0s | pass |
| `test_batch_prove.py` | 0 | 6.4s | pass |
| `test_brain_reachable.py` | 0 | 48.3s | pass |
| `test_bridge_discovery.py` | 0 | 0.0s | pass |
| `test_bridge_roundtrip.py` | 1 | 0.0s | **NOT RUN** — test host not built, needs .NET SDK |
| `test_caller_values.py` | 0 | 0.0s | pass |
| `test_capability.py` | 0 | 2.6s | pass |
| `test_carried_sources.py` | 0 | 6.2s | pass |
| `test_catalog.py` | 0 | 3.6s | pass |
| `test_config_and_health.py` | 0 | 0.0s | pass |
| `test_context.py` | 0 | 8.1s | pass |
| `test_diagnose.py` | 0 | 23.7s | pass |
| `test_embed.py` | 0 | 12.7s | pass |
| `test_failure_analysis.py` | 0 | 0.0s | pass |
| `test_fragment_imports.py` | 0 | 0.0s | pass |
| `test_fragment_needs.py` | 0 | 1.6s | pass |
| `test_fragment_needs_reader.py` | 0 | 1.7s | pass |
| `test_fragment_store.py` | 0 | 2.3s | pass |
| `test_gaps.py` | 0 | 0.0s | pass |
| `test_generate_jobs.py` | 0 | 21.2s | pass |
| `test_golden.py` | 0 | 0.0s | pass |
| `test_graph.py` | 1 | 17.0s | **FAIL** — 3 checks, pre-existing |
| `test_heron_guard.py` | 0 | 0.2s | pass |
| `test_licence_check.py` | 0 | 0.4s | pass — but see drive-letter limitation, §2 |
| `test_matrix.py` | 0 | 0.0s | pass |
| `test_mcp_serves.py` | 3 | 0.1s | **NOT RUN** — self-skip, no MCP SDK; exit 3 is by design |
| `test_mcp_stdio.py` | 0 | 0.0s | pass |
| `test_measure_brain.py` | 0 | 0.1s | pass |
| `test_measure_routes.py` | 0 | 0.3s | pass |
| `test_reachable.py` | 1 | 1.3s | **FAIL** — 1 check, pre-existing |
| `test_retrieve.py` | 0 | 25.9s | pass |
| `test_revit_gate.py` | 0 | 2.0s | pass |
| `test_scope_store.py` | 0 | 10.8s | pass |
| `test_search.py` | 0 | 17.6s | pass |
| `test_served_claims.py` | 1 | 0.0s | **NOT RUN** — import fails, no MCP SDK |
| `test_session_binding.py` | 0 | 0.0s | pass |
| `test_skills.py` | 0 | 5.9s | pass |
| `test_tool_registry.py` | 0 | 0.0s | pass |
| `test_validate_agent.py` | 0 | 4.3s | pass |
| `test_workflow.py` | 0 | 1.5s | pass |
| `test_write_safety.py` | 0 | 0.0s | pass |

### Totals, stated in the four states the plan requires

- **PASS: 36**
- **FAIL: 2** — `test_graph.py`, `test_reachable.py`. Both pre-existing, both unrelated to housekeeping.
- **NOT RUN (environment): 3** — `test_bridge_roundtrip.py` and `test_served_claims.py` and `test_mcp_serves.py`, all for a missing optional dependency.
- **NEEDS REAL REVIT: 0** among the suites.

### Delta against the state recorded in plan §28.11 item B

§28.11 B recorded 37 pass / 4 fail on the owner's Windows checkout at `69edf63`. This run
measures a **different set**, which is the exact hazard that item warned about.

| Suite | §28.11 B record | This run | Cause of the difference |
|---|---|---|---|
| `test_context.py` | fail, 1 check | **pass** | Newly passing. Not caused by this run — no file has been changed |
| `test_licence_check.py` | fail, crash | **pass** | The repair, plus a same-filesystem environment that cannot re-expose the defect |
| `test_reachable.py` | fail, 5 checks | fail, **1 check** | Still failing, but fewer checks. Needs a look before Phase I trusts either number |
| `test_graph.py` | fail, 3 checks | fail, 3 checks | Unchanged |
| `test_bridge_roundtrip.py` | pass | **not run** | Owner's machine had the test host built; this one has no .NET SDK |
| `test_mcp_serves.py` | pass | **not run** | Owner's machine had the MCP SDK; this one does not |
| `test_served_claims.py` | not listed | **not run** | Same missing MCP SDK |

Two suites changed state in a direction nobody claimed, and three changed only because the
machine changed. A total alone would have concealed all five.

---

## 5. `check-gaps.py` — exit 1 is the correct result

Duration 236.3s, exit 1, no suite hit the 300s bound. The gate behaved correctly.

It reports **3 UNFINISHED**: `test_graph.py`, `test_reachable.py`, `test_served_claims.py`.
Its exit code follows the UNFINISHED list only.

Two notes for whoever reads this later:

1. `test_served_claims.py` appears as UNFINISHED rather than WAITING, although its cause is
   the same missing MCP SDK that puts `test_mcp_serves.py` in WAITING. Recorded as an
   observation about the tool's classification, **not fixed** — plan §28.8 makes a defect
   found during cleanup a record, not a repair.
2. `check-gaps.py` runs **40** of the 41 suites. `test_bridge_roundtrip.py` is skipped
   deliberately, with the reason written in the code: it needs the compiled test host, and
   `check-compile.py` covers the compiling. So "41 suites" and "what check-gaps ran" are
   two different numbers, on purpose.

It also reports 49 WAITING items — 47 needing a real Revit, 1 needing the MCP SDK, 1 needing
the owner — and 193 fragments below PROVEN. **All of that is open product work, not a
housekeeping pass or failure** (plan §28.6).

---

## 6. Pre-existing defects recorded, deliberately not fixed

Plan §28.8: a defect found during cleanup is recorded and left.

| Defect | Evidence | Status |
|---|---|---|
| `test_graph.py` fails 3 checks | Suite output at baseline | Pre-existing, unrelated, **not fixed** |
| `test_reachable.py` fails 1 check | Suite output at baseline | Pre-existing, unrelated, **not fixed** |
| 4 broken local Markdown links | `check-docs.py` §1 | Pre-existing, **in scope for Phase G** |
| `check-gaps.py` classifies a missing-SDK failure as UNFINISHED | `check-gaps.py` output | Recorded, **not fixed** |

The four broken links, exactly as reported:

| File | Target |
|---|---|
| `docs/DECISIONS.md` | `12-security-and-privacy.md` |
| `docs/DECISIONS.md` | `06-agent-hr.md` |
| `docs/34-patterns-adapted.md` | `../brain/heron_health.py` |
| `docs/OPEN-QUESTIONS.md` | `12-security-and-privacy.md` |

---

## 7. Inventory scope rule, fixed now before Phase A classifies anything

Per plan §28.11 item E, the Phase A inventory is built from **tracked files at commit
`5aa7f6d`**. At this baseline the working tree is clean and there are **no untracked paths**,
so no "present, not in scope, owned elsewhere" rows are needed.

Two things §28.11 E flagged are already resolved at this baseline and need no action:

- `.gitignore` now re-includes `.agents/skills/*/bin/` as well as the older
  `.claude/skills/*/bin/` path, with the reason written beside it.
- All six skills under `.agents/skills/` are tracked. No `.pyc` is tracked anywhere.

Staging rule for every commit in this run: **explicit paths only, never `git add -A`.**

---

## 8. Next exact action

Phase B: reconcile the stale claims in §11 against derived truth. Nothing in Phase B moves or
deletes a file.

---

# PHASE A — repository-wide audit

Built from **tracked files at `5aa7f6d`**, per §28.11 item E. Working tree clean; no untracked
paths, so no "present, not in scope" rows are needed.

## 9. Inventory

1,329 tracked files at the baseline (1,330 including this record).

| Area | Files | Class | Disposition |
|---|---|---|---|
| `brain/fragments/` | 1,080 | Permanent source | **Grouped keep** — 360 fragments, all well-formed |
| `brain/skills/` | 10 | Permanent source | **Grouped keep** — all 10 `DRAFT` |
| `brain/*.py` | 13 | Permanent source | **Grouped keep** |
| `tools/` | 47 | Permanent source | **Grouped keep** (README classified separately) |
| `tests/` | 44 | Permanent source | **Grouped keep**; no README — Phase E candidate |
| `revit/` | 19 | Permanent source | **Grouped keep** |
| `mcp/` | 12 | Permanent source | **Grouped keep** |
| `platform/` | 11 | Permanent source | **Grouped keep** |
| `.codex/` | 5 | Permanent AI config | **Keep** — `config.toml` + 4 agent definitions |
| `.claude/` | 12 | Permanent AI config | **Keep, but see the conflict in §12** |
| `.agents/` | 7 | Permanent AI config | **Defer — see the conflict in §12** |
| `.github/` | 4 | Permanent config | **Keep** — issue templates only; no workflows, no PR template |
| Root config | 5 | Permanent config | **Keep** — `.gitignore`, `.mcp.json`, `Directory.Build.props`, `LICENSE`, `NOTICE` |
| Markdown | 82 | mixed | Classified in §10 |

**There is no CI in this repository.** `.github/` holds issue templates and nothing else — no
workflows, no pull-request template. Recorded as a fact about the validation surface, not a defect
to fix here.

## 10. Markdown classification — all 82 accounted for

| Class | Files | Disposition |
|---|---|---|
| **Permanent authority** | `docs/00`, `00b`, `00c`, `00d`, `00e`, `docs/01`–`34` (35), `HERON_CONSTITUTION.md`, `docs/DECISIONS.md` | **Keep.** Update links/status only |
| **Governance** | `CONTRIBUTING.md`, `CODE_OF_CONDUCT.md`, `SECURITY.md`, `DISCLAIMER.md` | **Keep** |
| **Research brief, authority already settled** | `HERON_AI_MASTER_ARCHITECTURE.md` | **Keep** — see §13 |
| **Navigation** | `README.md`, `docs/README.md`, `brain/README.md`, `mcp/README.md`, `platform/README.md`, `revit/README.md`, `tools/README.md`, `.claude/skills/README.md`, `brain/proof-drafts/README.md` | **Keep; three need updating** — §11 |
| **Live operational register** | `docs/NEEDS-CHECKING.md`, `docs/FRAGMENT-ISSUES.md`, `docs/OPEN-QUESTIONS.md`, `docs/PROPOSALS.md`, `docs/ROADMAP.md` | **Keep** — all actively maintained |
| **Historical evidence** | `brain/retrieval-history.md` | **Keep** — measurement history, explicitly quotable |
| **Session/work note** | `docs/HANDOVER.md` | **Defer the move** — §12 |
| **One-time execution prompt** | `docs/PROMPT-fragment-validation-agent.md` | **Retain with a remaining item** — §13 |
| **One-time plan, partly implemented** | `docs/FRAGMENT-REVIEW-PLAN-CHATGPT-2026-09-07.md` | **Retain; needs an inbound link** — §13 |
| **AI instruction** | 6 `.agents/skills/*/SKILL.md`, 6 `.claude/skills/*/SKILL.md`, 4 `.claude/agents/*.md` | **Defer** — duplication conflict, §12 |
| **This run's own files** | the housekeeping plan, this record | Plan deleted at Phase L only |

**Missing entirely, and named by the plan as deliverables:** `AGENTS.md` (§10), `docs/PROJECT-MAP.md`
(§11), `docs/work-notes/README.md` (§12), `tests/README.md` (§17 candidate).

## 11. Stale status claims found, with derived truth beside them

Derived by the command the README itself names:
`grep -h '^heron-status:' brain/fragments/*/fragment.yaml | sort | uniq -c`

**Truth at `5aa7f6d`: 360 fragments — 167 `PROVEN`, 193 `DRAFT`. Ten skills, all `DRAFT`.**

| File | Line | Claim | Truth | Verdict |
|---|---|---|---|---|
| `README.md` | 52 | "159 of the 360 fragments are `PROVEN` as of 2026-09-09" | 167 | **STALE** |
| `README.md` | 82 | "201 of the 360 fragments are `DRAFT` and 159 are `PROVEN`" | 193 / 167 | **STALE** |
| `README.md` | 45 | "41 test suites, all passing bar three … two want the MCP SDK and one wants a built .NET test host" | Measured 36 pass, 2 genuine fail, 3 not run | **STALE** — the count is wrong, and the reasons omit two real failures |
| `docs/README.md` | 5 | "159 of the 360 fragments are `PROVEN` as of 2026-09-09" | 167 | **STALE** |
| `brain/README.md` | 35 | "360 as of 2026-09-08 — 308 `DRAFT`, 52 `PROVEN`" | 193 / 167 | **STALE**, and the worst of the three |
| `brain/README.md` | 56 | "52 fragments are `PROVEN`; the other 308 … are `DRAFT`" | 167 / 193 | **STALE** |
| `docs/HANDOVER.md` | 80 | "41 suites. **38 pass** in a plain Linux container and the three failures are the MACHINE" | 36 pass in this Linux container; 3 machine + **2 genuine** | **INCOMPLETE** — the 3 machine failures it names are exactly right, but `test_graph` and `test_reachable` are missing from the account |

**`docs/HANDOVER.md` line 77 and line 1314 are correct** — both already say 167 `PROVEN`, 193 to go,
dated 2026-09-10. The operational note is current while the two front doors are not, which is the
opposite of what a reader would assume.

Claims checked and found **correct**, so Phase B must not touch them: "52 answered · 1 open"
(`check-docs` agrees, `Q-51` open), 30 Constitution Articles, 21 Golden Rules, 250 agents in the
registry, "all ten skills are `DRAFT`".

## 12. Conflicts needing an owner decision

### C-1 — `.claude/skills/` and `.agents/skills/` are near-duplicate copies of the same six skills

Commit `fc7aec1` states *"the skills have since moved to `.agents/skills/`"*. **They were copied, not
moved:** `.claude/skills/` still holds all six plus its own `README.md` and `bin/heron_guard.py`, all
tracked. Five of the six `SKILL.md` files are byte-identical across the two trees.

Every consumer still points at `.claude/skills/`: `tests/test_heron_guard.py`,
`tools/check-licence.py`, `tools/batch-prove.py`, `tools/generate-jobs.py`,
`tools/check-api-surface.py`, `tools/jobs/example.yaml`, `tools/README.md`, and several docs.
**Nothing except `.gitignore` and the housekeeping plan references `.agents/`.**

This is an incomplete migration, and which tree is canonical is a product-intent decision.
**Deferred to Ajmal** (§28.3: uncertain ownership → defer/keep). Not touched.

### C-2 — `.agents/skills/heron-guard/SKILL.md` points at a path that does not exist

It is the one file of the six that differs, and the difference is the hook command:

| Tree | Command in `SKILL.md` |
|---|---|
| `.claude/skills/heron-guard/` | `python .claude/skills/heron-guard/bin/heron_guard.py` |
| `.agents/skills/heron-guard/` | `python .Codex/skills/heron-guard/bin/heron_guard.py` |

**There is no `.codex/skills/` directory at all** — `.codex/` holds `config.toml` and four agent
`.toml` files. The path is also capitalised `.Codex`, which resolves on Windows and fails on Linux.
The script itself is byte-identical in both trees.

So the `.agents/` copy of a **deny-tier, fails-closed** hook names an executable that is not there.
Recorded as a defect per §28.8. **Not fixed** — it belongs with the C-1 decision.

### C-3 — `docs/HANDOVER.md` is 433 KB and is the documented cold-start entry point

Plan §13 asks whether it should move to `docs/work-notes/handover/`. It has 13 inbound references,
the root README sends every new reader to it, and its own first line is the sentence the owner is
told to type: *"Read HANDOVER.md in Heron-AI and carry on."*

§13 and §28.9 both allow keeping an established entry point in place. **Deferred pending the
Phase C responsibility map** — a move here would break saved continuation prompts that live outside
this repository and cannot be checked from inside it.

## 13. Files the plan names by name, resolved

| File | Plan section | Finding | Disposition |
|---|---|---|---|
| `HERON_AI_MASTER_ARCHITECTURE.md` | §22 | **Already resolved.** It carries a banner added 2026-09-09 under D-57: *"This is a research brief. It is not part of the Heron AI specification and supersedes nothing"*, pointing to `docs/32` for the audit | **No action.** §22 is satisfied by work that predates this run |
| `docs/PROMPT-fragment-validation-agent.md` | §14, §28.9 | Header says **BUILT 2026-09-06**. Verified: `brain/heron_validate.py` exists, `tests/test_validate_agent.py` exists and **passes** at baseline, `brain/proof-drafts/README.md` exists, the `validate` subcommand exists in `heron_bridge_client.py`. But the header also says *"Nothing has been run against Revit"* | **Retain.** §28.9 is explicit: any unproven required item means the prompt stays, with the remaining item named. Remaining item: the on-model half, which **NEEDS REAL REVIT**. Its filename says `PROMPT-`, which reads as an active job — a naming question for Phase C, not a deletion |
| `docs/FRAGMENT-REVIEW-PLAN-CHATGPT-2026-09-07.md` | §14 | Its own status line: C03, C04, C09 and N01–N09 done; **C01, C02, C05–C08 and S01–S05 remain plan only**. Its internal snapshot says 350 fragments; there are now 360 — a labelled dated snapshot, acceptable under §6 | **Retain — work is unfinished.** But it has **zero inbound references from anywhere in the repository**, so live remaining work sits in a file nothing links to. Giving it an inbound link is Phase G work |

## 14. Phase A exit evidence

- Every in-scope file is accounted for: 1,329 tracked files, grouped or classified.
- All **82** Markdown files classified; none left unread that is a move or delete candidate.
- No move, merge or delete has been performed. Three conflicts are deferred with reasons.
- Independent review: **pending**.
