# Session archive

**Finished work. Nothing here is specification, and nothing here is a live obligation.**

These are the session handover notes that were held inside [`../HANDOVER.md`](../HANDOVER.md) until
2026-09-12, when that file was 4390 lines longer than it needed to be and the thing a
reader actually wanted — what to do next — sat behind forty-four finished sessions.

Where a note here disagrees with [DECISIONS.md](../DECISIONS.md), the
[Golden Rules](../14-golden-rules.md) or the [Constitution](../../HERON_CONSTITUTION.md), **those win.**
A note records what was true on its own day and was never edited afterwards.

## What these are for

The registers carry what is still owed — decisions, defects, open questions, unchecked claims. **These
notes carry the narrative instead**: what was tried first, what failed, why an approach was dropped.
That is genuinely useful and it is genuinely not a register entry, which is why the notes were kept
rather than deleted.

If you are picking up work, you do not need to read any of them. Start at
[`../HANDOVER.md`](../HANDOVER.md).

## Adding a new one

**One file per sitting.** Name it `YYYY-MM-DD-short-topic.md` and add one row at the top of the table
below. Keep only what is NEW, a MISTAKE worth not repeating, and what is still TO DO; anything owed goes
to its register and a GitHub issue too. Do not write a new section into `../HANDOVER.md` — that is the race
this folder exists to remove.

## Keeping this folder small

**Only recent sittings are kept here.** On 2026-11-24, at the owner's request, the 141 notes older than
2026-11-11 were deleted: their durable parts were already in the registers (as this page always said),
and every one is still in git history. To read an old note:

```bash
git log --diff-filter=D --name-only --oneline -- docs/handover-archive/
git show <commit>^:docs/handover-archive/<file-name>.md
```

When this table passes about ten rows, delete the oldest the same way, after checking that anything
still owed in them has a register row.

## The sittings

| Date | Sitting |
|---|---|
| 2026-11-24 | [THE CLEAN-UP OF 2026-11-24: DOCS CUT, IDLE AGENTS RETIRED](2026-11-24-the-clean-up-docs-cut-and-idle-agents-retired.md) |
| 2026-11-24 | [THE LONG HANDOVER, KEPT WHOLE WHEN HANDOVER.MD WAS CUT TO ONE PAGE](2026-11-24-the-long-handover-kept-whole-when-handover-was-cut-to-one-page.md) |
| 2026-11-21 | [THE MERGES OF 2026-11-21: THE NEW WORK WAITS FOR REVIT, AND FIVE QUESTIONS WAIT FOR THE OWNER](2026-11-21-the-day-s-merges-the-new-work-waits-for-revit-and-five-questions-for-the-owner.md) |
| 2026-11-14 | [A NIGHT ON REVIT 2024 ALONE: SCRATCH MODELS HERON MADE, TEN DRAFTS AND SEVEN DEFECTS](2026-11-14-a-night-on-revit-2024-alone-scratch-models-heron-made-and-seven-defects.md) |
| 2026-11-13 | [THE OWNER'S PC PROVES WHAT #855 BUILT, AND THE PROVING FINDS NEW DEFECTS ON THE WAY](2026-11-13-the-owner-s-pc-proves-what-855-built-and-the-proving-finds-new.md) |
| 2026-11-12 | [EVERY REPLY SAYS HOW IT ENDED IN ONE WORD, PROVEN TOOLS THAT ANSWERED WRONGLY ARE REPAIRED AND DRAFT AGAIN, AND A DEFECT ROW GETS A FILE OF ITS OWN](2026-11-12-every-reply-says-how-it-ended-in-one-word-proven-tools-that.md) |
| 2026-11-11 | [A CLICK IN REVIT ANSWERS THE CHAT, FIVE EVERYDAY JOBS GET A METHOD, AND A CLOUD CHAT NOBODY SET UP GETS A LIBRARY](2026-11-11-a-click-in-revit-answers-the-chat-five-everyday-jobs-get-a.md) |
