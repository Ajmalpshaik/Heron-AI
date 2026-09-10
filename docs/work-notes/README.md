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

### Two things that are deliberately NOT in this folder

**[`docs/HANDOVER.md`](../HANDOVER.md) is the current-work entry point, and it stays where it is.**
It is operational — it is exactly the kind of thing this folder is for — but it is also the documented
cold start: the root README sends every new reader to it, thirteen files link to it, and its own first
line is the sentence the owner types to resume work. Saved continuation prompts pointing at that path
live outside this repository and cannot be checked from inside it. Moving it would cost more than the
tidiness is worth. **Read it as if it were in this folder.**

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
| `investigations/` | Research and debugging notes from one session | Create when needed |

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

| File | State |
|---|---|
| [`plans/FRAGMENT-REVIEW-PLAN-CHATGPT-2026-09-07.md`](plans/FRAGMENT-REVIEW-PLAN-CHATGPT-2026-09-07.md) | **Active, partly implemented.** An outside review of the fragment library. C03, C04, C09 and N01-N09 are done; **C01, C02, C05-C08 and the S01-S05 splits are still plan only.** Its baseline numbers are a dated snapshot - derive current ones. Moved here from `docs/` on 2026-09-10: it is a one-time plan somebody is executing, which is what this folder is for |
| [`plans/PROMPT-fragment-validation-agent.md`](plans/PROMPT-fragment-validation-agent.md) | **Active, one half remaining.** The brief that built the Fragment Validation Agent. The agent exists and its test passes; **the on-model half NEEDS REAL REVIT** and is why the prompt is retained rather than deleted. Moved here from `docs/` on 2026-09-10 |
| [`plans/housekeeping-execution-record.md`](plans/housekeeping-execution-record.md) | **Permanent.** The ledger for the repository housekeeping run — baseline, gate results, every disposition and its evidence. **The plan that drove it was deleted on 2026-09-10** once its closure rules were met; this record is what survives, and it quotes the plan's requirements where it relies on them |
