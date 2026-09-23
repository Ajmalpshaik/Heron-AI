# Taking the best of the earlier brain into Heron — the plan folder

> **Type:** Operational work note. **Not specification.** Where a sentence here disagrees with the
> [Constitution](../../../../HERON_CONSTITUTION.md), the [Golden Rules](../../../14-golden-rules.md) or
> [DECISIONS.md](../../../DECISIONS.md), **those win and this note is out of date.**
> **Status:** **Active — being built package by package.** The status of every package, and of PR
> [#299](https://github.com/Ajmalpshaik/Heron-AI/pull/299), lives in
> [`02-work-packages.md` §0](02-work-packages.md) **and nowhere else.** Opened 2026-09-23. **Owner:** Ajmal PS.

---

## What this is

On 2026-09-22 the owner asked for **everything** in his earlier brain repository to be checked — not only
its hooks — and for only what makes Heron better to be brought across:

> *"do not take blindly … it needs to be part of Heron, connect everything, nothing needs to be broken"*

He also chose **where** it is built: in Claude Code cloud sessions, because both repositories are on
GitHub, and then back on his PC with Revit to test what was built.

**Provenance, cited once and nowhere else in this folder:** the earlier library is
`github.com/Ajmalpshaik/AJ-AI-Brain`, read at commit `f5570b3` on the owner's PC on 2026-09-22 — the same
commit [08](../plugin-extension/08-lessons-from-the-brain.md) read. It is read-only reference
([HANDOVER](../../../HANDOVER.md): *read it, never edit it, never commit to it*). **Nothing in this folder
carries its names, paths, files or wording**, and nothing built from this plan may either —
[D-25](../../../DECISIONS.md), [31](../../../31-studying-the-existing-libraries.md),
[AGENTS.md](../../../../AGENTS.md). A cloud session does **not** need that repository: everything it
needs is in this folder.

**Two earlier reads are not repeated here.** Its fragment library was classified in
[31 §1](../../../31-studying-the-existing-libraries.md) — every eligible file read, every gap built — and
its working habits were taken in [08](../plugin-extension/08-lessons-from-the-brain.md).

## Read in this order

| | |
|---|---|
| [`01-findings.md`](01-findings.md) | **What was checked, and what it found.** Nine areas, what Heron already does better, what was deliberately not taken, and the dangers the comparison exposed in Heron itself |
| [`02-work-packages.md`](02-work-packages.md) | **The plan.** Eleven packages — what each builds, where it lives, what it must connect to so nothing breaks, how it is proved, and the order they run in |
| [`03-owner-data.md`](03-owner-data.md) | **The owner's own data that existed only on his PC** — his site words, his real questions, his grayout and practice values — carried here so a cloud session can use them |
| [`04-cloud-prompts.md`](04-cloud-prompts.md) | **One prompt per package**, ready to paste into a cloud session, plus the rules every session follows |
| [`05-pc-proving.md`](05-pc-proving.md) | **What comes back to the PC** once the cloud has built it: deploying, the owner's data going into his own stores, and every proof that needs a real Revit |

## The rules of this folder

1. **Every claim carries its evidence** — a file and line, a decision number, or the command that derives
   it. A sentence with no source is an opinion and does not belong here.
2. **VERIFIED, REPORTED and DECIDED are different words and are never mixed.** A claim re-checked by hand
   on 2026-09-22/23 says **verified**. A claim a reader made and nobody re-checked says **reported**, and
   the session that acts on it checks it first.
3. **Numbers carry the day they were measured and the command that derives them.** They move; the command
   does not.
4. **The owner's data in [03](03-owner-data.md) is not product content.** Heron ships empty stores — the
   Keyword Agent's own rule is that it *"ships NO LIST"* ([`brain/heron_keywords.py`](../../../../brain/heron_keywords.py),
   [D-34](../../../DECISIONS.md)). His words and his standards go into **his** stores on **his** PC
   ([05](05-pc-proving.md)); this folder only carries them there.

## Closure condition

Every package in [02](02-work-packages.md) is **DONE with evidence** or **withdrawn with a reason**; every
proof in [05](05-pc-proving.md) is recorded against a named model; the owner's data in
[03](03-owner-data.md) is in his own stores; the durable knowledge has moved to the registers, the skills
and the fragments it belongs to — **and this folder is deleted.**
