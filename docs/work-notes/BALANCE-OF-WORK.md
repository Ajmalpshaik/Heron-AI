<!-- Heron-Agent:  none -->
<!-- Heron-Step:   18 -->
<!-- Heron-Status: DRAFT -->
<!-- Heron-Since:  0.1.0 -->
<!-- Heron-Layer:  docs -->
<!-- See docs/29-metadata-standard.md -->

# The balance of work

> **GENERATED 2026-10-27 08:11 by `python tools/balance-of-work.py --write`.**
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
| 1 | **Fragments that have never met a model** — the largest single body of work left | **71** of **605** | `grep -h '^heron-status:' brain/fragments/*/fragment.yaml \| sort \| uniq -c` |
| 1a | — of those, never run **in this checkout** — see the warning below | **71** | `python tools/generate-jobs.py` |
| 1b | — of those, **ready to prove right now**, needing only a Revit session | **42** | `python tools/generate-jobs.py` |
| 1c | — of those, **structurally blocked**, each with a reason printed | **29** | `python tools/generate-jobs.py` |
| 2 | **Skills never proved** | **35** of **35** | `grep -h '^heron-status:' brain/skills/*.yaml \| sort \| uniq -c` |
| 3 | **Agents left to build** | **0** of **252** | `python tools/agent-count.py` |
| 3b | — and **deferred by a decision**, which is neither left nor done | **2** | `python tools/agent-count.py` |
| 4 | **Agent proofs drafted but unsigned** | **0** | `ls brain/agent-proof-drafts/*.yaml` |
| 5 | **Proving-register rows still open** | **1553** of **1763** | `python tools/owner-queue.py` |
| 6 | **Heron's own defects still open** | **261** of **901** | `python tools/open-defects.py` |
| 7 | **Questions unanswered** | **18** | `python tools/check-docs.py` |
| 8 | **Proposals awaiting the owner** | **21** of **25** | `python tools/owner-queue.py` |
| 9 | **Signatures gone stale — proved, then the code moved under them** | **7** | `python tools/check-signatures.py` |
| 10 | **Everything waiting on the owner personally**, across three registers | **1592** | `python tools/owner-queue.py` |

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

**Questions still open:** Q-58, Q-60, Q-61, Q-69, Q-70, Q-71, Q-72, Q-73, Q-74, Q-75, Q-76, Q-77, Q-78, Q-79, Q-80, Q-81, Q-82, Q-85

**Defects still open** (`docs/FRAGMENT-ISSUES.md` section 5): 10, 18, 19, 21, 32, 36, 41, 45, 73, 75, 99, 101, 103, 106, 108, 116, 120, 128, 129, 131, 132, 135, 137, 140, 141, 142, 143, 144, 146, 148, 150, 158, 174, 176, 177, 179, 180, 5b-83, 5b-95, 5b-103, 5b-105, 5b-107, 5b-110, 5b-113, 5b-125, 5b-131, 5b-136, 5b-145, 5b-154, 5b-159, 5b-161, 5b-175, 5b-180, 5b-182, 5b-185, 5b-187, 5b-188, 5b-192, 5b-194, 5b-197, 5b-200, 5b-202, 5b-204, 5b-203, 5b-205, 5b-211, 5b-212, 5b-213, 5b-214, 5b-215, 5b-216, 5b-218, 5b-219, 5b-221, 5b-222, 5b-224, 5b-225, 5b-226, 5b-227, 5b-230, 5b-233, 5b-235, 5b-237, 5b-240, 5b-246, 5b-253, 5b-256, 5b-257, 5b-258, 5b-259, 5b-260, 5b-265, 5b-278, 5b-280, 5b-281, 5b-282, 5b-285, 5b-286, 5b-287, 5b-288, 5b-289, 5b-290, 5b-291, 5b-294, 5b-295, 5b-296, 5b-297, 5b-299, 5b-301, 5b-302, 5b-303, 5b-304, 5b-314, 5b-320, 5b-321, 5b-323, 5b-331, 5b-336, 5b-337, 5b-338, 5b-340, 5b-342, 5b-346, 5b-348, 5b-350, 5b-351, 5b-352, 5b-356, 5b-358, 5b-361, 5b-364, 5b-367, 5b-378, 5b-396, 5b-402, 5b-403, 5b-414, 5b-415, 5b-416, 5b-418, 5b-419, 5b-420, 5b-393, 5b-394, 5b-395, 5b-423, 5b-424, 5b-427, 5b-428, 5b-429, 5b-434, 5b-437, 5b-444, 5b-448, 5b-455, 5b-469, 5b-472, 5b-473, 5b-474, 5b-475, 5b-476, 5b-477, 5b-478, 5b-490, 5b-496, 5b-513, 5b-594, 5b-650, 5b-665, 5b-666, 5b-861, 5b-940, 5b-977, 5b-1003, 5b-1004, 5b-1007, 5b-1060, 5b-1061, 5b-1062, 5b-1063, 5b-1065, 5b-1066, 5b-1090, 5b-1091, 5b-1092, 5b-1095, 5b-1100, 5b-1110, 5b-1111, 5b-1112, 5b-1113, 5b-1114, 5b-1115, 5b-1117, 5b-1118, 5b-1119, 5b-1120, 5b-1121, 5b-1122, 5b-1123, 5b-1124, 5b-1125, 5b-1126, 5b-1127, 5b-1128, 5b-1130, 5b-1131, 5b-1132, 5b-1133, 5b-1134, 5b-1135, 5b-1136, 5b-1138, 5b-1153, 5b-1154, 5b-1155, 5b-1156, 5b-1157, 5b-1158, 5b-1159, 5b-1160, 5b-1161, 5b-1162, 5b-1163, 5b-1164, 5b-1165, 5b-1166, 5b-1167, 5b-1168, 5b-1169, 5b-1170, 5b-1171, 5b-1172, 5b-1173, 5b-1174, 5b-1175, 5b-1176, 5b-1177, 5b-1178, 5b-1179, 5b-1180, 5b-1181, 5b-1182, 5b-1183, 5b-1201, 5b-1217, 5b-1220, 5b-1221, 5b-1242, 5b-1261, 5b-1262, 5b-1267, 5b-1268, 5b-1292, 5b-1293, 5b-1309, 5b-1310, 5b-1324, 5b-1329, 5b-1335, 5b-1340

> A row can still say OPEN after a later row has closed it — four did on 2026-09-16, and no
> pattern finds them. Reading beats grepping here.

**Proposals still open** (`docs/PROPOSALS.md`): F1, F2, F4, F6, F7, F8, F9, F10, F11, F12, F13, F14, F16, F18, F19, F20, F21, F22, F23, F42, F43

## Work notes still open, and what each is waiting for

A note is not finished when its work is finished. It is finished when the durable part of it
has moved somewhere permanent and the note itself has gone. These have not reached that point,
and the reason is the second column — read it before deleting one.

| Note | Why it is still here |
|---|---|
| `plans/FRAGMENT-REVIEW-PLAN-CHATGPT-2026-09-07.md` | Active, partly implemented |
| `plans/PROMPT-fragment-validation-agent.md` | Built, and both halves have run - it looks finished |
| `plans/rag/00-structure.md` | Active — the shape is decided, and parts are built |
| `plans/rag/01-requirements.md` | Active, partly built |
| `plans/rag/02-implementation.md` | Active, and some stages are built |
| `the-sweep-2026-09-13.md` | Active, and a MAP rather than a proof — its first line says so |
| `plans/improvement-gate-execution-record.md` | Active — the gate is built and piloted; two things are still owed |
| `investigations/rag-engineering-practices-2026-09-10.md` | All four take-rows taken; three owner's calls open |
| `investigations/rag-state-of-the-art-2026-09-10.md` | Read and sorted; three rows folded in, and its one question answered |
| `plans/agent-build-order-2026-09-13.md` | Every block built - retiring it is the owner's call |
| `plans/next-steps-2026-09-12.md` | Active, and mostly spent |
| `investigations/2026-09-17-risk-crossings-not-deterministic.md` | Active, and pinned open by a register |
| `plans/earlier-brain/README.md` | Nearly spent — every package merged or dropped except C8; the status is in [02 §0](plans/earlier-brain/02-work-packages.md) |
| `plans/earlier-brain/01-findings.md` | Complete as a record |
| `plans/earlier-brain/02-work-packages.md` | Active — the only copy of the packages' status |
| `plans/earlier-brain/03-owner-data.md` | Active, and not product content |
| `plans/earlier-brain/04-cloud-prompts.md` | Used, except C8's prompt |
| `plans/earlier-brain/05-pc-proving.md` | Waiting on the PC, not on the cloud |
| `plans/family-modelling-2026-10-01.md` | Active - eight stacked pull requests built 2026-10-01, and a ninth 2026-10-02 |
| `plans/plugin-extension/README.md` | Active, and a folder of its own |
| `plans/building-loads-2026-10-04.md` | A plan whose code exists - not proven |
| `plans/sprinkler-panel-2026-10-04.md` | FINAL plan (its own banner), and its code exists - not proven |
| `plans/fire-water-and-spacing-2026-10-05.md` | FINAL plan (its own banner), and its code exists - not proven |
| `plans/sprinkler-layout-2026-10-06.md` | FINAL plan (its own banner), and its code exists - not proven |
| `plans/project-review-2026-10-08.md` | Active |
| `plans/gap-fill-2026-10-12.md` | Active - being built batch by batch |

---

## What this page deliberately does not cover

- **Whether anything above is a good idea.** It counts what is open, not what is worth doing.
- **Anything with no register.** If a piece of work is in nobody's list, it is in no row here
  either, and that is a gap in the registers rather than in this page.
- **The archive.** `docs/handover-archive/` is finished work kept on purpose and is never a
  balance.

