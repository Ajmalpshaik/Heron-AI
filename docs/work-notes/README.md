# Work notes

**This folder is operational workspace. Nothing in it is product specification.**

If a sentence here disagrees with the specification, the [Constitution](../../HERON_CONSTITUTION.md),
the [Golden Rules](../14-golden-rules.md) or [DECISIONS.md](../DECISIONS.md), **those win** and the note
is out of date.

Its purpose is one question: *what work is going on right now?* Open this folder and you should be able
to answer it without reading the whole repository.

---

## What lives here, and what does not

| Goes here | Does **not** go here |
|---|---|
| A one-time plan somebody is executing | Accepted architecture — that is `docs/00`–`34` |
| A temporary note about a specific defect | An accepted decision — that is [DECISIONS.md](../DECISIONS.md) |
| A raw idea not yet worth proposing | A reviewed proposal — that is [PROPOSALS.md](../PROPOSALS.md) |
| A research or debugging note from one session | A question that must be answered — that is [OPEN-QUESTIONS.md](../OPEN-QUESTIONS.md) |
| An execution record with its evidence | An unproven claim — that is [NEEDS-CHECKING.md](../NEEDS-CHECKING.md) |

**THE TEST IS DELETION, NOT HOW OPERATIONAL SOMETHING FEELS.** Everything here ends at *retired* -
see the lifecycle below - and *retired* means deleted. So the question for any file is: **will this
be deleted once its work is done?** If yes, it belongs here. If it is where a note's knowledge goes
to SURVIVE, it does not.

That is why the registers in the right-hand column live in `docs/` and not here. They are never
deleted; they are the permanent destination this folder empties into. Filing them here would put the
destination inside the bin.

**[`docs/PROJECT-MAP.md` §D](../PROJECT-MAP.md) groups them all as "only context", and that is about
AUTHORITY, not about location.** By authority the registers, the handover and these notes are one
kind of thing. By lifecycle they are two. Mixing the two questions is how a register nearly ended up
in here.

### Two things that are deliberately NOT in this folder

**[`docs/HANDOVER.md`](../HANDOVER.md) is the current-work entry point, and it stays where it is.**
It is operational — it is exactly the kind of thing this folder is for — but it is also the documented
cold start: the root README sends every new reader to it, **sixteen files reference it — fourteen
documents and two tools, at 2026-09-10** — and its own first line is the sentence the owner types to
resume work. Do not trust that number either. It is exactly the kind this page's own rule says to
derive rather than type, so derive it:

```bash
grep -rl 'HANDOVER\.md' --include='*.md' --include='*.py' . | grep -v '\.git/' | grep -v 'docs/HANDOVER\.md'
```

Saved continuation prompts pointing at that path live outside this repository and cannot be checked
from inside it. Moving it would cost more than the tidiness is worth. **Read it as if it were in
this folder.**

**Ideas that have been reviewed belong in [`PROPOSALS.md`](../PROPOSALS.md), not here.** That file
already owns reviewed gaps, risks and suggestions. `ideas/` here is only for a raw thought that is not
yet worth putting in front of anyone.

---

## The subfolders

Only folders that actually hold something exist. **Do not pre-create the empty ones** — an empty
folder is a promise the repository has not kept.

| Folder | For | Exists today |
|---|---|---|
| `plans/` | One-time implementation plans and execution prompts, plus the execution record of one | ✅ |
| `handover/` | A handover note that is **not** the main `docs/HANDOVER.md` — a side thread, a parallel session | Create when needed |
| `fixes/` | Temporary instructions for one specific defect | Create when needed |
| `ideas/` | Raw ideas, before they are ready for `PROPOSALS.md` | Create when needed |
| `investigations/` | Research and debugging notes from one session | ✅ |

---

## The lifecycle every note here follows

```text
active  →  blocked or completed  →  knowledge preserved  →  retired
```

**A note is not finished when its work is finished.** It is finished when the durable part of it has
been moved somewhere permanent and the note itself has gone.

1. **Active** — somebody is working from it. Say who, and what the closure condition is.
2. **Blocked or completed** — say which. A blocked note names what it is waiting for and who can
   unblock it. It does not sit here looking active.
3. **Knowledge preserved** — before deleting anything, ask what is in it that exists nowhere else:
   a constraint discovered the hard way, an approach that failed and why, the reasoning behind a
   choice. Put that in the permanent document that owns the topic, then check it reads correctly
   there. **Git history is recovery evidence, not discoverable knowledge.**
4. **Retired** — delete it, and repair whatever linked to it.

A note kept for historical value is **labelled historical** and links to whoever owns the topic now.
It does not go on issuing instructions.

---

## Rules that have already cost this repository something

**Never mark work done because a note says so.** Check the code, the tests, or the tool. This whole
folder exists because a status typed into Markdown goes stale and a command does not.

**Never type a count that a command can derive.** Say where it comes from instead:

```bash
grep -h '^heron-status:' brain/fragments/*/fragment.yaml | sort | uniq -c   # fragment lifecycle
python tools/check-gaps.py                                                  # unfinished vs waiting
python brain/heron_fragment.py                                              # fragment count and shape
```

Three README files in this repository carried a fragment count that was wrong by more than a hundred,
each with a sentence beside it admitting the number goes stale. Writing the sentence is not enough.

**A defect found while tidying is recorded, not fixed.** Widening a cleanup into a code change is how
a reviewable batch becomes an unreviewable one.

---

## Who keeps this true, and when

Maintenance is **proportional** — it is triggered by a change, not by a calendar.

| When this happens | Whoever did it | Does this |
|---|---|---|
| Behaviour, a path, or a status changes | the person who changed it | Update the **canonical page for that module** and any derived report. Not every page that mentions it — the one that owns it |
| A work note's work finishes | the task owner | Disposition review: move the durable part somewhere permanent, repair references, then delete the note |
| A blocked note's dependency changes | the task owner | Re-read it. A blocked note that is no longer blocked is an active one, and should say so |
| Before a release, or handing the project to a new reader | the reviewer | Repeat the navigation and link checks, and re-read the entry documents for stale claims |

The link and count checks are already executable, so run them rather than reading for them:

```bash
python tools/check-docs.py        # links, and every count that can be derived
python tools/check-metadata.py
python tools/check-structure.py
```

**Two rules that exist because this repository has been bitten by them.**

A **stale count is not a small defect.** Three README files once disagreed with the tools by more than
a hundred fragments at the same time, each with a sentence beside the number admitting it would go
stale. The sentence did not help. Name the command instead of the number.

**A feature documented as missing is worse than one documented as broken.** Two files said running a
fragment that writes did not exist, months after it did and after 55 of them had been proven against a
real model. Nobody looks for what they have been told is not there.

---

## What is here now

> **This table listed ELEVEN notes on 2026-09-16 and FIFTEEN were on disk.** The four it had
> never gained are at the bottom, added that day. It is a typed list of files in a folder -
> the exact shape this page's own rules forbid twice, in a file whose subject is that rule.
> The State column is the reason it survives: **a state cannot be derived, but the LIST can**,
> so check the list before trusting the states.
>
> ```bash
> # every note on disk, against every note this table names
> cd docs/work-notes
> find . -name '*.md' ! -name README.md | sed 's|^\./||' | sort > /tmp/on-disk.txt
> grep -oE '\]\([A-Za-z0-9/._-]+\.md\)' README.md | tr -d '](' | tr -d ')' | grep -v '^\.\.' | sort -u > /tmp/listed.txt
> diff /tmp/on-disk.txt /tmp/listed.txt && echo 'the table is complete'
> ```

| File | State |
|---|---|
| [`plans/FRAGMENT-REVIEW-PLAN-CHATGPT-2026-09-07.md`](plans/FRAGMENT-REVIEW-PLAN-CHATGPT-2026-09-07.md) | **Active, partly implemented.** An outside review of the fragment library. C03, C04, C09 and N01-N09 are done; **C01, C02, C05-C08 and the S01-S05 splits are still plan only.** Its baseline numbers are a dated snapshot - derive current ones. Moved here from `docs/` on 2026-09-10: it is a one-time plan somebody is executing, which is what this folder is for |
| [`plans/PROMPT-fragment-validation-agent.md`](plans/PROMPT-fragment-validation-agent.md) | **Active, one half remaining.** The brief that built the Fragment Validation Agent. The agent exists and its test passes; **the on-model half NEEDS REAL REVIT** and is why the prompt is retained rather than deleted. Moved here from `docs/` on 2026-09-10 |
| [`plans/rag/00-structure.md`](plans/rag/00-structure.md) | **Active — the shape is decided, nothing is built.** What kind of RAG Heron is building: six structural decisions, taken together by the owner on 2026-09-10. **Read it before the other three** — structure decides requirements, not the other way round. Two of the six turn out to be reporting rather than machinery, because [D-01](../DECISIONS.md) already gave the host the classifying and the answering. Becomes a numbered decision when agreed |
| [`plans/rag/01-requirements.md`](plans/rag/01-requirements.md) | **Active, nothing built from it yet.** What Heron’s RAG has to do, gathered from the specification so a build need not re-read nine documents — 95 numbered requirements (R-01 to R-93, plus R-88a and R-88b), each with its source and its state today. Opened 2026-09-10. Retires when every row is DONE with evidence or withdrawn with a reason, its durable half moving into [`docs/05`](../05-heron-brain.md) and [DECISIONS.md](../DECISIONS.md) |
| [`plans/rag/02-implementation.md`](plans/rag/02-implementation.md) | **Active, no stage started.** How each stage is built — what it closes, what proves it, and what it deliberately does not do. **Its stage ORDER is superseded by [`00-structure.md`](plans/rag/00-structure.md) §6**; the stage contents are still the detail. **Nothing in it needs Revit or the PC**, which is why it can run while a model is not open |
| ~~`plans/rag/03-working-note.md`~~ | **RETIRED 2026-09-12 — 158 KB and 2,383 lines, and the second note retired the right way this month.** Its durable half was moved first and nothing was summarised in place: the **first retrieval measurement ever taken on the trained backend** (2026-09-10, 360 fragments) is now a dated section of [`brain/retrieval-history.md`](../../brain/retrieval-history.md), which is where it always belonged; `W-8` became [NEEDS-CHECKING](../NEEDS-CHECKING.md) **A15**; `W-10` became [PROPOSALS](../PROPOSALS.md) **F7**; and `Q-D` and `Q-E` became [OPEN-QUESTIONS](../OPEN-QUESTIONS.md) **Q-54** and **Q-55** — **two questions that had been owed since 2026-09-10 and 2026-09-11 while that register said one was open.** `W-9` was already `A10` and `W-7` already sits in [`00-structure.md` §3.3](plans/rag/00-structure.md); `W-1` to `W-6` were closed, and `S-1`, `S-2`, `S-4`, `Q-A`, `Q-B`, `Q-C` answered. **A question living in a deletable file is the failure this row is the evidence for** |
| [`the-sweep-2026-09-13.md`](the-sweep-2026-09-13.md) | **Active, and a MAP rather than a proof — its first line says so.** Every DRAFT fragment run once on 2026-09-13, positive phase only, against one arrangement. **Three minutes for 161.** What it changes is the reading of the backlog: 135 of the 147 outstanding are blocked by a VALUE nobody had said, not by a thin model, and each refusal names which one. It also records the trap that cost five hours of nothing — `validate` PROMPTS at the keyboard for a negative arrangement, so a script must close stdin. **Retires when the last row in its table has a value supplied**, or sooner if a richer model makes the table wrong. Owner: whoever next proves fragments |
| [`handover/proving-session-2026-09-13.md`](handover/proving-session-2026-09-13.md) | **Active, and the shortest-lived thing here — read it, then delete it.** The handover for 2026-09-13: 199 → **204 PROVEN**, and **six promotions reversed the same day** after a review caught what the signing gate cannot see. Its durable half is already in [`FRAGMENT-ISSUES.md`](../FRAGMENT-ISSUES.md) rows 17–22 and [NEEDS-CHECKING](../NEEDS-CHECKING.md) A16b. The finding that outranks the rest: **a rollback that reports success sometimes does not happen** — intermittent, and no count can see it. **Delete once the next session has read it** |
| [`handover/proving-session-2026-09-11.md`](handover/proving-session-2026-09-11.md) | **Active, and the shortest-lived thing here — read it, then delete it.** The handover for the proving session of 2026-09-10 into 2026-09-11: 175 → **197 PROVEN**, twenty-two promoted, and **eleven defects** found. Its durable half is already in [`FRAGMENT-ISSUES.md`](../FRAGMENT-ISSUES.md), which is the whole point — this note carries only what a *next session* needs and does not survive being read. Two things in it are owed rather than done: **seven promoted proofs name a model they were not taken against**, and whether the risk gate belongs on `validate` is an open question worth a `D-` number. It also names the six traps that cost this session time, including the one where a proof said "Snowdon" and meant a different Snowdon |
| [`plans/improvement-gate-execution-record.md`](plans/improvement-gate-execution-record.md) | **Active — the gate is built and piloted; four things are owed.** The ledger for the Improvement Gate: the Phase 0 audit of what Heron already had, one note per phase, and the evidence each produced. Its durable half is already moved — [D-68](../DECISIONS.md) and [D-69](../DECISIONS.md) carry the decisions, [34 §2.15–2.19](../34-patterns-adapted.md) the external patterns, [13 §3a](../13-testing-and-quality.md) the offline-vs-real-Revit line. What it still holds is **three defects recorded rather than fixed**, and the honest note that the gate has only ever been run by the session that wrote it, which [Golden Rule 7](../14-golden-rules.md) says is not enough. **Retires once somebody who did NOT build it has used the gate** — that has still not happened, so it stays. The housekeeping record that sat beside it **was retired on 2026-09-12**: its last open item, the cold read, moved to [NEEDS-CHECKING](../NEEDS-CHECKING.md) **R3**, and the Windows-only suite failure it found became **A14** and was fixed. That is the lifecycle working — knowledge preserved, then the note gone |
| [`investigations/rag-engineering-practices-2026-09-10.md`](investigations/rag-engineering-practices-2026-09-10.md) | **All four take-rows taken; three owner's calls open.** Five repositories on one outside profile, read at the owner’s request for anything useful to the whole platform rather than only RAG. **Four ✅ take, three ⏸ owner’s call, the entire stack ❌ does not transfer** — every storage and orchestration piece in it needs a service or a server, which [D-01](../DECISIONS.md) forbids. The one worth having is a **fabrication check that calls no model at all**, which turns *no source, no claim* into a number measurable offline. It also **corrected [`plans/rag/00-structure.md`](plans/rag/00-structure.md)**: [34 §2.13](../34-patterns-adapted.md) had already measured a third retrieval stream at six settings and rejected it. Retires into [`docs/34`](../34-patterns-adapted.md) when the owner takes or refuses each row |
| [`investigations/rag-state-of-the-art-2026-09-10.md`](investigations/rag-state-of-the-art-2026-09-10.md) | **Read and sorted; three rows folded in, one large question left open.** The second reading, wider than one profile: LightRAG, RAGFlow, contextual retrieval, Docling, current chunking practice for regulated documents, CPU re-rankers and the embedded vector stores. **Five Heron decisions survived it** — including [D-23](../DECISIONS.md), which now has a **stated ceiling of roughly 500,000 vectors** instead of an implied one. The finding worth having: the published way to situate a chunk needs a model call per chunk, and **for a numbered standard the heading path does the same job for free** — so [D-24](../DECISIONS.md) is not re-broken. Carries **S-4**, the Docling dependency question, with **no default offered.** Retires into [`docs/34`](../34-patterns-adapted.md) |
| [`handover/mep-session-2026-09-16.md`](handover/mep-session-2026-09-16.md) | **Active, and the newest thing here — read it, then delete it.** The handover for 2026-09-16: **298 → 310 `PROVEN`**, 372 total, thirteen signed by Ajmal PS and all thirteen promoted. **It was not a proving session, it was a JOB** - a copper refrigerant pipe type for split AC units in his own naming convention and colour - and twelve fragments exist because that sentence could not be carried out with the library as it stood. Every one was proved against the model the job was done in rather than an arrangement invented for the proof, which is the stronger kind. Its durable half is in [`FRAGMENT-ISSUES.md`](../FRAGMENT-ISSUES.md) rows 95–99. **Added to this table 2026-09-16; it had been missing since the day it was written** |
| [`handover/proving-session-2026-09-15.md`](handover/proving-session-2026-09-15.md) | **Active, scaffolding, read it then delete it.** The handover for 2026-09-14/15: **291 → 298 `PROVEN`**, seven signed and promoted. Its finding is that **not one of the seven was blocked by anything in a model** - every block was the machinery, and each a different shape. Durable half already in [`FRAGMENT-ISSUES.md`](../FRAGMENT-ISSUES.md) rows 89–94, [D-72](../DECISIONS.md) and [`tools/jobs/`](../../tools/jobs/). **Added to this table 2026-09-16** |
| [`plans/agent-build-order-2026-09-13.md`](plans/agent-build-order-2026-09-13.md) | **Active.** The order that makes each agent easier to build than the one before it, written 2026-09-13 at the owner's request. It continues [`next-steps-2026-09-12`](plans/next-steps-2026-09-12.md) Track B item B4, which said only *"keep building agents"*. **It writes no numbers at all** - the snapshot it was drafted against moved within the same day, and it says so as its argument. Retires when every block is built or has moved into a register. **Added to this table 2026-09-16** |
| [`plans/next-steps-2026-09-12.md`](plans/next-steps-2026-09-12.md) | **Active.** The two tracks - using Heron on one side while building it on the other - and the answer to the question that prompted it: **yes, and they do not collide**, because the risk classes the library already declares keep them apart. For the long-term phases read [ROADMAP.md](../ROADMAP.md) instead; this covers the next stretch only. Retires when every step is done or has moved into a register. **Added to this table 2026-09-16** |
