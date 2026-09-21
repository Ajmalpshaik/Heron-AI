<!-- Heron-Agent:  none -->
<!-- Heron-Step:   18 -->
<!-- Heron-Status: DRAFT -->
<!-- Heron-Since:  0.1.0 -->
<!-- Heron-Layer:  docs -->
<!-- See docs/29-metadata-standard.md -->

# The balance of work

> **GENERATED 2026-09-21 05:57 by `python tools/balance-of-work.py --write`.**
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
| 1 | **Fragments that have never met a model** — the largest single body of work left | **68** of **395** | `grep -h '^heron-status:' brain/fragments/*/fragment.yaml \| sort \| uniq -c` |
| 1a | — of those, never run **in this checkout** — see the warning below | **68** | `python tools/generate-jobs.py` |
| 1b | — of those, **ready to prove right now**, needing only a Revit session | **29** | `python tools/generate-jobs.py` |
| 1c | — of those, **structurally blocked**, each with a reason printed | **39** | `python tools/generate-jobs.py` |
| 2 | **Skills never proved** | **10** of **10** | `grep -h '^heron-status:' brain/skills/*.yaml \| sort \| uniq -c` |
| 3 | **Agents left to build** | **0** of **250** | `python tools/agent-count.py` |
| 3b | — and **deferred by a decision**, which is neither left nor done | **2** | `python tools/agent-count.py` |
| 4 | **Agent proofs drafted but unsigned** | **0** | `ls brain/agent-proof-drafts/*.yaml` |
| 5 | **Proving-register rows still open** | **130** of **187** | `python tools/owner-queue.py` |
| 6 | **Heron's own defects still open** | **32** of **216** | `python tools/open-defects.py` |
| 7 | **Questions unanswered** | **0** | `python tools/check-docs.py` |
| 8 | **Proposals awaiting the owner** | **21** of **22** | `python tools/owner-queue.py` |
| 9 | **Signatures gone stale — proved, then the code moved under them** | **2** | `python tools/check-signatures.py` |
| 10 | **Everything waiting on the owner personally**, across three registers | **151** | `python tools/owner-queue.py` |

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

**Defects still open** (`docs/FRAGMENT-ISSUES.md` section 5): 10, 18, 19, 21, 32, 36, 41, 45, 73, 75, 99, 101, 103, 108, 116, 120, 128, 129, 131, 132, 133, 135, 137, 140, 141, 142, 143, 144, 146, 148, 151, 158

> A row can still say OPEN after a later row has closed it — four did on 2026-09-16, and no
> pattern finds them. Reading beats grepping here.

**Proposals still open** (`docs/PROPOSALS.md`): F1, F2, F4, F5, F6, F7, F8, F9, F10, F11, F12, F13, F14, F15, F16, F17, F18, F19, F20, F21, F22

## Work notes still open, and what each is waiting for

A note is not finished when its work is finished. It is finished when the durable part of it
has moved somewhere permanent and the note itself has gone. These have not reached that point,
and the reason is the second column — read it before deleting one.

| Note | Why it is still here |
|---|---|
| `plans/FRAGMENT-REVIEW-PLAN-CHATGPT-2026-09-07.md` | Active, partly implemented |
| `plans/PROMPT-fragment-validation-agent.md` | Active, one half remaining |
| `plans/rag/00-structure.md` | Active — the shape is decided, nothing is built |
| `plans/rag/01-requirements.md` | Active, nothing built from it yet |
| `plans/rag/02-implementation.md` | Active, no stage started |
| `the-sweep-2026-09-13.md` | Active, and a MAP rather than a proof — its first line says so |
| `plans/improvement-gate-execution-record.md` | Active — the gate is built and piloted; four things are owed |
| `investigations/rag-engineering-practices-2026-09-10.md` | All four take-rows taken; three owner's calls open |
| `investigations/rag-state-of-the-art-2026-09-10.md` | Read and sorted; three rows folded in, one large question left open |
| `plans/agent-build-order-2026-09-13.md` | Active |
| `plans/next-steps-2026-09-12.md` | Active |
| `investigations/2026-09-17-risk-crossings-not-deterministic.md` | Active, and pinned open by a register |

---

## What this page deliberately does not cover

- **Whether anything above is a good idea.** It counts what is open, not what is worth doing.
- **Anything with no register.** If a piece of work is in nobody's list, it is in no row here
  either, and that is a gap in the registers rather than in this page.
- **The archive.** `docs/handover-archive/` is finished work kept on purpose and is never a
  balance.

