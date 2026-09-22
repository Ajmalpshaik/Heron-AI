# 04 — The cloud prompts

> **Type:** Operational work note. **Not specification.** Part of [the earlier-brain plan](README.md).
> **Status:** Ready 2026-09-23. **Owner:** Ajmal PS.

---

**How to use this page.** Each package in [02](02-work-packages.md) has one prompt below. Open a new Claude
Code **cloud** session on the `Ajmalpshaik/Heron-AI` repository, paste the prompt, and answer its questions.
**Wave 1 — C1, C2, C3, C4 and C6 — can run at the same time**, one session each. Start a Wave 2 or Wave 3
prompt only when the package it needs has merged ([02 §0](02-work-packages.md)).

When a session finishes it gives you a draft pull request. **Bring the pull-request number back to a
session on your PC** to be checked before you merge — work handed to another session is verified when it
comes back, not trusted.

---

## §0 — What every session does, whichever package it is

Every prompt below begins by pointing here. This is the part that keeps Heron unbroken.

1. **Where you are.** A Claude Code cloud session: Linux, the repository on GitHub, **no Revit and no
   access to the owner's PC**. The environment is the one [38](../../../38-the-cloud-environment.md)
   describes. If `HERON_KNOWLEDGE` is not set, run
   `export HERON_KNOWLEDGE="$HOME/heron-kb" && mkdir -p "$HERON_KNOWLEDGE"` before anything touches the
   brain. The `revit_*` tools will say there is no session — that is correct here, not a fault.
2. **Start from today's `main`.** `git fetch origin`, then read the titles of every commit on
   `origin/main` since 2026-09-23. **If your package's work has already landed, stop and say so.** (On
   2026-09-22 a whole pull request duplicated one merged an hour earlier.) If this plan folder is not on
   `main` yet, it is on the branch `claude/earlier-brain-plan`. Branch from `origin/main` as
   `claude/eb-<package>-<few-words>`.
3. **Read before you touch anything:** [`AGENTS.md`](../../../../AGENTS.md); this folder's
   [README](README.md), [01](01-findings.md) and **your package's section of [02](02-work-packages.md)**;
   [03](03-owner-data.md) if your package uses the owner's data; the README of every folder you will
   change; and the house-rule skills that apply — [`heron-ship`](../../../../.claude/skills/heron-ship/SKILL.md)
   always, [`revit-addin-conventions`](../../../../.claude/skills/revit-addin-conventions/SKILL.md) for
   anything under `revit/`, [`fragment-proving`](../../../../.claude/skills/fragment-proving/SKILL.md) for
   anything under `brain/fragments/`.
4. **Check before you change.** Every claim in 02 marked *reported* is checked in the file first. If the
   file disagrees with the plan, **the file wins**: say so, and do not force the plan.
5. **Stay inside your package.** Change only the files your section of 02 says it owns, plus what they
   connect to so nothing breaks — a new tool gets its registry row, its test, its README row, and every
   document that counts it. **If you find a defect elsewhere, record it; do not fix it.**
6. **Never copy from the earlier library.** You do not need it. No name, path or wording from it may enter
   Heron ([D-25](../../../DECISIONS.md)).
7. **The registers.** Another session may still be splitting `docs/FRAGMENT-ISSUES.md`,
   `docs/NEEDS-CHECKING.md`, `docs/HANDOVER.md` and `docs/DECISIONS.md`. If that split has **not** merged,
   do not edit those four: write the rows you owe into your pull request under **"Register rows owed"**.
   If it **has** merged, follow the new layout and take the next free id at the moment you write.
8. **Prove it.** A new check is seen to **fail** on the old code before it passes. Run
   `tools/check-change.py` with a `tools/change-evidence.py` record, and all **ten** gates in
   `heron-ship` §1 — the four local ones and the six CI adds. A green board is not a Revit proof: anything
   that needs Revit goes into your report under **"Needs the PC"**, with the named-model proof it owes
   ([D-30](../../../DECISIONS.md), [05](05-pc-proving.md)).
9. **Never merge, and never widen permission.** Stage files by explicit path, never `git add -A`. Open a
   **draft** pull request. `write.enabled` stays `false`.
10. **Talking to Ajmal.** He is a BIM modeller, not a developer. **Ask one question at a time**, in plain
    words, with a Revit comparison where it helps; recommend an answer. Never ask him to choose between
    two technical implementations — decide that yourself and say why.
11. **Finish with this report, in plain words:** what was built; the evidence (the commands and what they
    printed); **Needs the PC** (exactly what to prove, on what kind of model); **Register rows owed**; the
    pull-request link; and what is left, as a number.
12. **Update [02 §0](02-work-packages.md)** — your package's State cell only — in the same pull request.

---

## Wave 1 — these five can run at the same time

### C1 — Guards and checks

```text
You are package C1 of Heron's earlier-brain plan, in a Claude Code cloud session on Ajmalpshaik/Heron-AI.

First read and follow docs/work-notes/plans/earlier-brain/04-cloud-prompts.md §0 (if the folder is not on main,
it is on branch claude/earlier-brain-plan). Then do exactly docs/work-notes/plans/earlier-brain/02-work-packages.md
§3 "C1 — Guards and checks that run in every session".

In short, and in this order:
1. Wire the existing heron-guard hook from a committed .claude/settings.json so it runs in EVERY session (today it
   runs only after its skill is loaded - proven 2026-09-22, see 01 §3 H2). Remove the frontmatter hooks block so it
   runs once. Update tests/test_heron_guard.py, the skill, docs/34 §2.9 and a dated line on Q-49 so every
   description is true.
2. A "has main moved?" hook: before a pull request is merged or marked ready, fetch origin/main with a timeout and
   tell the AI which commits the branch does not have. Advice only - it never blocks.
3. One status line at session start: branch vs origin/main, proven/draft fragment counts. Fast, silent on failure,
   works in Git Bash on Windows and on Linux.
4. Every hook logs its own decisions to a local file outside the repository (use Heron's path helper, never a typed
   path), plus a small report of how often each fired.
5. Three checks in tools/check-docs.py: tools/ and .claude/skills/ fully named in their READMEs (add the 9 missing
   tools rows in the same change); text hygiene over git ls-files (control characters, double-encoded text,
   conflict markers - build forbidden bytes with chr()); the MCP tool total derived from heron_tools.TOOLS, and the
   "14 MCP tools" lines in docs/33 and docs/34 corrected. Measure check-docs' time before and after.

Every hook is unit-tested by piping JSON into it, the way tests/test_heron_guard.py already does. Every check is seen
to fail on a planted fault first. You own only the files 02 lists for C1. Nothing here touches mcp/, revit/ or brain/.
When done, give the §0.11 report.
```

### C2 — The MCP server

```text
You are package C2 of Heron's earlier-brain plan, in a Claude Code cloud session on Ajmalpshaik/Heron-AI.

First read and follow docs/work-notes/plans/earlier-brain/04-cloud-prompts.md §0 (if the folder is not on main,
it is on branch claude/earlier-brain-plan). Then do exactly docs/work-notes/plans/earlier-brain/02-work-packages.md
§3 "C2 — The MCP server".

BEFORE ANY CODE, ask Ajmal this one question (decision D1), in plain words, and wait for his answer:
"When you ask Heron to change the model, revit_change keeps the change straight away, with no preview. Heron's
own rulebook (Constitution Article 9) says: show a preview first - what will change and how many - and keep it
only after you say yes. Which do you want: (A) add the preview, so every change shows its counts first and waits
for your yes - I recommend this; or (B) keep changing straight away, and write that down as an official exception
to Article 9?"
Also check first whether PR #299 (the risk refusal in revit_change) has merged - build on it, never around it.

Then, in this order:
1. Implement D1's answer, so the code and Article 9 agree at the end.
2. A read-only door: one new tool that runs READ/ANALYZE fragments through the add-in's run_fragment_read, works
   with Changes OFF, refuses anything above ANALYZE, aims at the pinned document, and prints the result with the
   fragment's proof status. Register it in heron_tools.TOOLS, correct the server text that says reads can already
   run, and add it to heron-model-auditor's tool list (tests/test_agent_tools.py). First check DECISIONS and
   OPEN-QUESTIONS for any decision that keeps reads off MCP on purpose.
3. Rules that reach the AI in every chat: pass instructions to the server, assembled by brain/heron_instructions.py
   (Articles assembled from the Constitution, never copied). Keep them short. Include: use Revit's own Undo, never a
   reversing change; never say "compliant" - the engineer or authority decides; look up an unfamiliar word and ask
   when it is not recorded. Make the Constitution's Enforcement table true.
4. MCP annotations on every tool, derived from heron_tools.TOOLS, with a test that no label disagrees with the
   registry.

You own mcp/server/, brain/heron_instructions.py, heron-model-auditor's tool list (both hosts), their tests and
docs/04. When done, give the §0.11 report.
```

### C3 — Add-in safety and the API lookup

```text
You are package C3 of Heron's earlier-brain plan, in a Claude Code cloud session on Ajmalpshaik/Heron-AI.

First read and follow docs/work-notes/plans/earlier-brain/04-cloud-prompts.md §0 (if the folder is not on main,
it is on branch claude/earlier-brain-plan). Then do exactly docs/work-notes/plans/earlier-brain/02-work-packages.md
§3 "C3 — Add-in safety, and an API signature lookup". Read .claude/skills/revit-addin-conventions/SKILL.md first.

In this order:
1. Revit failure handling for fragment writes: RevitFragment.cs has none, RevitWrite.cs (the move path) does. Give
   the fragment path the same discipline - count and dismiss warnings, roll back on errors, never let Revit's
   default resolution apply (it can delete elements), report what was dismissed. One undo per job. Compile on all
   eight releases with tools/check-compile.py (install dotnet-sdk-10.0 if the environment has not).
2. In the write pre-check, skip an element that is not current with central, with the reason - the same way an
   element owned by another user is skipped. Confirm the API member exists on every release with
   tools/check-api-surface.py.
3. A --members <Type> mode in tools/api-surface/ printing parameters, return type, get/set and [Obsolete] for all
   eight releases, from the package copies tools/api-changes.py already caches. No Revit needed.

Nothing here can be proven without Revit: say exactly what the PC must prove, on what kind of model, in "Needs the
PC" (see 05-pc-proving.md). You own only the files 02 lists for C3. When done, give the §0.11 report.
```

### C4 — Revit traps into the fragments

```text
You are package C4 of Heron's earlier-brain plan, in a Claude Code cloud session on Ajmalpshaik/Heron-AI.

First read and follow docs/work-notes/plans/earlier-brain/04-cloud-prompts.md §0 (if the folder is not on main,
it is on branch claude/earlier-brain-plan). Then do exactly docs/work-notes/plans/earlier-brain/02-work-packages.md
§3 "C4 — Revit traps", rows 1 to 9. Read .claude/skills/fragment-proving/SKILL.md first.

Rules that matter most here:
- Each trap was observed elsewhere, mostly on Revit 2020. Write it as "observed elsewhere, not yet proven here".
- A note added to a PROVEN fragment's card keeps its proof. ANY change under a PROVEN fragment's impl/ makes the
  proof stale - list every such fragment under "Needs the PC" so it is re-proved on a named model.
- Every row marked "reported" in 02 is checked in the fragment's own files before you change anything. If the file
  disagrees with the plan, the file wins - say so.
- Row 7: read open PR #300 (category-override read-back) before touching the colour fragments.
- Row 1 first: connect-air-terminals says "NOTHING IS MOVED" - correct that, read each terminal's position before
  and after, and REPORT any movement. Do not invent a tolerance - an unknown number is a question for Ajmal.

You own only the fragment folders named in rows 1 to 9. When done, give the §0.11 report.
```

### C6 — A score for Ajmal's real questions

```text
You are package C6 of Heron's earlier-brain plan, in a Claude Code cloud session on Ajmalpshaik/Heron-AI.

First read and follow docs/work-notes/plans/earlier-brain/04-cloud-prompts.md §0 (if the folder is not on main,
it is on branch claude/earlier-brain-plan). Then do exactly docs/work-notes/plans/earlier-brain/02-work-packages.md
§3 "C6 — A score for the owner's real questions". The 79 questions are in 03-owner-data.md §B.

In this order:
1. For each question, propose the capability or skill that SHOULD answer it, from Heron's capability list - never
   from what the search returns. Where nothing in Heron should answer it, say "no capability - a gap". Show Ajmal
   the whole table once and ask: "Is any of these wrong?" Take his corrections.
2. Put the questions and the confirmed answers under tests/ as data. Write tools/score-routing.py: ask each question
   through the same function heron_lookup uses; report first place, top three, found-but-low and not-found
   separately, exact matches separately, and any question that lands on a write; stamp date, fragment count and
   backend; append one row to brain/retrieval-history.md; print what moved since the last comparable run.
3. A test that plants a drop and proves the scorer catches it.
4. Ask Ajmal decision D3, one question: "Should a drop in this score only be reported, or should it stop a pull
   request from merging? Heron's other routing checks only report today - I recommend report-only to start."

Never add or weaken an utterance to win a row, and never rewrite a question to suit the search (FRAGMENT-ISSUES row
113, D-34). When done, give the §0.11 report.
```

---

## Wave 2 — start each one after the package it needs has merged

### C5 — His words and standards (after C2)

```text
You are package C5 of Heron's earlier-brain plan, in a Claude Code cloud session on Ajmalpshaik/Heron-AI.

First read and follow docs/work-notes/plans/earlier-brain/04-cloud-prompts.md §0. Check that package C2 has merged -
if not, stop and say so. Then do exactly 02-work-packages.md §3 "C5 — The owner's words and standards". The data is
in 03-owner-data.md §A, §C and §D.

1. Site words are KNOWLEDGE, not language (D-34's own consequences). Read brain/heron_keywords.py and PROPOSALS F16.
   Build its store in the owner's scope, an import that records reviewed entries with the person and the date, and
   a way for the host to look a word up that rewrites nothing. The product ships the store EMPTY - the 52 entries
   are imported on the owner's PC later (05-pc-proving.md). Use two or three of them as test fixtures only.
2. The grayout standard as a company-scope standard, offered by name as a question (D-83, D-33); the mep-grayout
   skill points at it. Make the whole grayout ONE undo by sending the add-in's setup write steps through
   revit_change - FRAGMENT-ISSUES row 141 governs it.
3. The practice values as a company-scope standard, offered by name - never a default.
4. Decision D4 - the four open grayout choices in 03 §C - ask Ajmal one at a time, recommending an answer each time.

When done, give the §0.11 report.
```

### C7 — A fragment-writing agent (after C3)

```text
You are package C7 of Heron's earlier-brain plan, in a Claude Code cloud session on Ajmalpshaik/Heron-AI.

First read and follow docs/work-notes/plans/earlier-brain/04-cloud-prompts.md §0. Check that package C3 has merged
(its API --members lookup is used here). Then do exactly 02-work-packages.md §3 "C7 — A fragment-writing agent".

Write the agent for both hosts (.claude/agents/ and .codex/agents/ - tests/test_host_agents.py holds them
together), with a tool list that contains nothing able to change the model or the chat, and a row in
docs/28-agent-registry.md. Its method: search first, widen an existing capability rather than add a second fragment
for one job, write the card and impl at DRAFT, compile on every release, run check-routing and
check-declared-questions, write the proof plan with a negative case, and never run anything on a live model.
Correct .claude/agents/skill-author.md, which calls itself the prototype of the Fragment Creation Agent. When done,
give the §0.11 report.
```

### C8 — Three fragments from the outside-library notes (after C4)

```text
You are package C8 of Heron's earlier-brain plan, in a Claude Code cloud session on Ajmalpshaik/Heron-AI.

First read and follow docs/work-notes/plans/earlier-brain/04-cloud-prompts.md §0. Check that package C4 has merged.
Then do exactly 02-work-packages.md §3 "C8 — Three fragments from the outside-library notes": ceiling and ray checks
that see linked models (follow move-to-ray-hit and check-obstructions; links only when asked, and say how many were
read - D-59); a "changed in central?" column in the ownership report fragments; a new placeholder-sheet fragment,
after recording that create-sheet-list's card claims CREATE_SHEET makes placeholders. Every "reported" claim is
checked first. New fragments start at DRAFT. When done, give the §0.11 report.
```

---

## Wave 3 — starts with Ajmal's answer to D2

### C9 — Skills that carry a method

```text
You are package C9 of Heron's earlier-brain plan, in a Claude Code cloud session on Ajmalpshaik/Heron-AI.

First read and follow docs/work-notes/plans/earlier-brain/04-cloud-prompts.md §0. Then do exactly 02-work-packages.md
§3 "C9 — Skills that carry a method".

BEFORE ANY CODE, ask Ajmal decision D2, in plain words: "Today a Heron skill is like a family with all its parameters
but no formulas: it lists which tools a job needs, but not the order or the checks between steps. I recommend adding
the step-by-step method inside each skill, so Heron knows the order and what to check before the next step. Shall I
do it that way?" Record his answer, then teach every reader of the skill format and make the method reach the host.

Then the HVAC methods, in order: terminal layout from the space's airflow (supply and return alternating - his
"zig zag"); space airflow (return airflow needs its mode set first); and a new duct-routing skill from FCU to
terminals, writing its missing capabilities with the fragment-writing agent from C7 (a branch builder, a duct end
cap, the reducer 200 mm after the takeoff). Every number is asked or offered by name from his standards. When done,
give the §0.11 report.
```

### C10 — Tagging and sprinkler methods (after C9)

```text
You are package C10 of Heron's earlier-brain plan, in a Claude Code cloud session on Ajmalpshaik/Heron-AI.

First read and follow docs/work-notes/plans/earlier-brain/04-cloud-prompts.md §0. Check that C9 has merged. Then do
exactly 02-work-packages.md §3 "C10 — Tagging and sprinkler methods": a tag-placement capability that chooses the
side well, with his tag values offered from C5's standard; and the sprinkler-layout method, whose limits come only
from the company's licensed NFPA edition in company scope (ask Ajmal whether it is loaded - never use second-hand
numbers), which never says "compliant", and which treats the hazard class as an input. When done, give the §0.11
report.
```

### C11 — Family creation (last)

```text
You are package C11 of Heron's earlier-brain plan, in a Claude Code cloud session on Ajmalpshaik/Heron-AI.

First read and follow docs/work-notes/plans/earlier-brain/04-cloud-prompts.md §0. Check that C7 and C9 have merged.
Then do exactly 02-work-packages.md §3 "C11 — Family creation, last", writing each missing capability with the
fragment-writing agent, each at DRAFT, each with a resize test that reads several values back as its proof plan.
When done, give the §0.11 report.
```
