<!-- Heron-Agent:  none -->
<!-- Heron-Step:   18 -->
<!-- Heron-Status: DRAFT -->
<!-- Heron-Since:  0.1.0 -->
<!-- Heron-Layer:  docs -->
<!-- See docs/29-metadata-standard.md -->

# The balance of work

> **GENERATED 2026-11-21 18:46 by `python tools/balance-of-work.py --write`.**
> **If that stamp is not today, this page is history and not a work list.** Every figure
> below was read back out of the tool that owns it, and each row names the command that
> derives it. Nothing here is typed by hand, so where this page and a register disagree,
> the register is right and this page is out of date. Run it again.

**It is a work note, and it is meant to be deleted** — which is why it lives in
`work-notes/` and not in `docs/`. When a row below reaches zero, that row has no more
business here. When every row does, delete the file and the tool with it.

---

## What is left

| # | What is left | Count | Derive it with |
|---|---|---|---|
| 1 | **Fragments that have never met a model** — the largest single body of work left | **127** of **746** | `grep -h '^heron-status:' brain/fragments/*/fragment.yaml \| sort \| uniq -c` |
| 1a | — of those, never run **in this checkout** — see the warning below | **127** | `python tools/generate-jobs.py` |
| 1b | — of those, **ready to prove right now**, needing only a Revit session | **102** | `python tools/generate-jobs.py` |
| 1c | — of those, **structurally blocked**, each with a reason printed | **25** | `python tools/generate-jobs.py` |
| 2 | **Skills never proved** | **50** of **50** | `grep -h '^heron-status:' brain/skills/*.yaml \| sort \| uniq -c` |
| 3 | **Agents left to build** | **0** of **252** | `python tools/agent-count.py` |
| 3b | — and **deferred by a decision**, which is neither left nor done | **2** | `python tools/agent-count.py` |
| 4 | **Agent proofs drafted but unsigned** | **0** | `ls brain/agent-proof-drafts/*.yaml` |
| 5 | **Proving-register rows still open** | **2837** of **3358** | `python tools/owner-queue.py` |
| 6 | **Heron's own defects still open** | **13** of **1399** | `python tools/open-defects.py` |
| 7 | **Questions unanswered** | **2** | `python tools/check-docs.py` |
| 8 | **Proposals awaiting the owner** | **11** of **37** | `python tools/owner-queue.py` |
| 9 | **Signatures gone stale — proved, then the code moved under them** | **67** | `python tools/check-signatures.py` |
| 10 | **Everything waiting on the owner personally**, across three registers | **2850** | `python tools/owner-queue.py` |

> **ROWS 1a TO 1c ARE ABOUT THIS CHECKOUT, NOT ABOUT THE PROJECT.** They count what has never
> been run *here*, and `brain/proof-drafts/runs/` is gitignored, so every worktree starts almost
> empty. On 2026-09-19 the main checkout held **309** run records and a fresh worktree held
> **5** — the same library read as *14 ready* from one and *35 ready* from the other. **Regenerate
> this page from the checkout you will actually prove in**, and treat a figure generated
> anywhere else as meaningless. Row 1 itself is committed data and does not have this problem.

**Rows 1 and 5 are not the same work and neither contains the other.** A fragment is proved
against a model; a register row is a thing nothing has checked. A session that clears one can
leave the other untouched.

## Read the ids, not the count

Every drift this repository has caught was visible in a list and invisible in a total.

**Questions still open:** Q-95, Q-96

**Defects still open** (`docs/FRAGMENT-ISSUES.md` section 5): 5b-1161, 5b-1643, 5b-1743, 5b-1833, 5b-1834, 5b-1836, 5b-1837, 5b-1838, 5b-1839, 5b-1840, 5b-1870, 5b-1883, 5b-1906

> A row can still say OPEN after a later row has closed it — four did on 2026-09-16, and no
> pattern finds them. Reading beats grepping here.

**Proposals still open** (`docs/PROPOSALS.md`): F43, F46, F47, F48, F49, F50, F51, F52, F53, F54, F55

## Work notes still open, and what each is waiting for

A note is not finished when its work is finished. It is finished when the durable part of it
has moved somewhere permanent and the note itself has gone. These have not reached that point,
and the reason is the second column — read it before deleting one.

| Note | Why it is still here |
|---|---|
| `plans/FRAGMENT-REVIEW-PLAN-CHATGPT-2026-09-07.md` | Active, partly implemented |
| `plans/rag/00-structure.md` | Active — the shape is decided, and parts are built |
| `plans/rag/01-requirements.md` | Active, partly built |
| `plans/rag/02-implementation.md` | Active, and some stages are built |
| `investigations/rag-engineering-practices-2026-09-10.md` | All four take-rows taken; three owner's calls open |
| `plans/agent-build-order-2026-09-13.md` | Every block built - retiring it is the owner's call |
| `investigations/2026-09-17-risk-crossings-not-deterministic.md` | Active, and pinned open by a register |
| `investigations/2026-11-01-export-setups.md` | Looked finished on 2026-11-11, and one card has gone back to DRAFT since; retiring it is the owner's call |
| `investigations/2026-11-01-schedule-settings.md` | Nearly finished - all but one of its cards say PROVEN (read 2026-11-11) |
| `plans/earlier-brain/README.md` | Nearly spent — every package merged, done or dropped, C8 too since it was read again on 2026-11-21; the status is in [02 §0](plans/earlier-brain/02-work-packages.md) |
| `plans/earlier-brain/01-findings.md` | Complete as a record |
| `plans/earlier-brain/02-work-packages.md` | Active — the only copy of the packages' status |
| `plans/earlier-brain/03-owner-data.md` | Active, and not product content |
| `plans/earlier-brain/04-cloud-prompts.md` | Used, except C8's prompt |
| `plans/earlier-brain/05-pc-proving.md` | Waiting on the PC, not on the cloud |
| `plans/family-modelling-2026-10-01.md` | Active - eight stacked pull requests built 2026-10-01, and a ninth 2026-10-02 |
| `plans/plugin-extension/README.md` | Active, and a folder of its own |
| `plans/building-loads-2026-10-04.md` | A plan whose code exists - not proven |
| `plans/sprinkler-panel-2026-10-04.md` | FINAL plan (its own banner), and its code exists - the panel is not proven |
| `plans/fire-water-and-spacing-2026-10-05.md` | FINAL plan (its own banner), and its code exists - not proven |
| `plans/sprinkler-layout-2026-10-06.md` | FINAL plan (its own banner), and its code exists - the panel is not proven |
| `plans/gap-fill-2026-10-12.md` | Active - being built batch by batch |
| `plans/companion-model-dashboard-2026-11-01.md` | Active - built well past phase 1; its own table holds each row's state |
| `plans/facade-capability.md` | Close to finished - its cards say PROVEN; retiring it is the owner's call |
| `plans/revit-sitting-2026-11-14.md` | Active |
| `plans/windows-setup-2026-11-16.md` | Active - design only, nothing built |
| `investigations/2026-11-11-family-2d-capabilities.md` | Active, and close - what keeps it is the owner's |
| `investigations/2026-11-15-revit-mcp-landscape.md` | Active - written for the owner to read, then delete |

---

## What this page deliberately does not cover

- **Whether anything above is a good idea.** It counts what is open, not what is worth doing.
- **Anything with no register.** If a piece of work is in nobody's list, it is in no row here
  either, and that is a gap in the registers rather than in this page.
- **The archive.** `docs/handover-archive/` is finished work kept on purpose and is never a
  balance.

