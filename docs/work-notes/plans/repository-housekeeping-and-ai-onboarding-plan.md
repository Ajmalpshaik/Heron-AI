# Heron AI — Repository Housekeeping, Documentation & AI Onboarding Master Plan

> **Type:** Temporary execution plan — revised 2026-09-10; execution not started
> **Scope of this revision:** Publish an improved plan only. Reading, editing, reviewing or pushing this file does not activate its execution prompt or self-removal rule.
> **Execution method:** Read section 28 before Phase A. Use the phase gates and task records below; choose inline or delegated execution only when housekeeping itself is authorized.
> **Repository:** `Ajmalpshaik/Heron-AI`
> **Purpose:** Clean, reconcile, restructure, document, and make the repository easy to understand for Ajmal, a BIM modeller, a BIM manager, a developer, or a fresh AI agent.
> **Important:** This file is temporary. **Delete this file only after every applicable task in this plan is completed, verified, and the final repository audit meets the closure rules in sections 25–28. No required failure may be waived merely by documenting it.**

---

## 1. Objective

Heron AI already contains a large amount of valuable code, specifications, architecture documents, decisions, handover notes, fragment evidence, tools, skills, tests, prompts, and status notes.

The problem to solve is not simply "make the folders neat".

The real goal is to make the repository behave like a well-managed BIM project:

- a new person can understand the project without opening every file;
- an AI agent can orient itself quickly before editing anything;
- permanent product documentation is separated from temporary work notes;
- current status is accurate and does not rely on stale typed numbers;
- completed one-time prompts and fix notes do not remain as unexplained clutter;
- important historical decisions are preserved;
- every major module explains what it contains and how it connects to the rest of Heron;
- no cleanup breaks code, links, tests, build paths, agent instructions, fragment metadata, or Revit compatibility;
- nobody has to guess which document is authoritative.

The final result must feel like entering a clean BIM model with correct worksets, naming, views, sheets, standards, and handover information already organized.

---

## 2. Non-negotiable rules

These rules apply to the entire housekeeping operation.

### 2.1 Study before changing

Do not reorganize files based only on names.

Before moving, deleting, merging, rewriting, or replacing any file:

1. Read it.
2. Understand why it exists.
3. Find all references to it.
4. Check whether its task is complete, active, blocked, historical, or authoritative.
5. Check whether another document already owns the same responsibility.
6. Decide its correct final classification.

### 2.2 Never invent project status

Do not mark anything `PROVEN`, `COMPLETE`, `TESTED`, `SUPPORTED`, `CLOSED`, or equivalent unless repository evidence proves it.

For Revit work, a successful compile is not the same as a real Revit proof.

Where Heron already has tools that derive status from disk, the tool output is stronger than a typed sentence in Markdown.

Examples already used by this repository include:

- `python tools/check-gaps.py`
- fragment `heron-status` values under `brain/fragments/`
- `python tools/agent-count.py`
- documentation validation tools
- compile gates
- tests
- real Revit proof records

Prefer derived truth over manually maintained totals.

### 2.3 Do not blindly copy or rewrite from external projects

Heron must remain Heron.

If outside repositories were studied, preserve only the ideas, mechanisms, scars, lessons, or architecture that were deliberately re-authored for this project.

Do not import another project's branding, file structure, dependencies, wording, licence assumptions, agent names, or architecture simply because it looks cleaner.

### 2.4 No destructive cleanup without proof

Do not delete a file because it looks old.

Delete only when one of these is true:

- the task it instructed is verified complete and its information exists in permanent documentation;
- it is a duplicate with no unique information;
- it is generated junk that is reproducible;
- it is an obsolete temporary prompt or note and no live references depend on it;
- the repository explicitly identifies it as disposable.

If a file contains unique lessons, decisions, failure evidence, Revit behavior, or design reasoning, preserve that knowledge in the correct permanent place before deleting the temporary file.

### 2.5 Preserve Git safety

- Do not force-push.
- Do not rewrite repository history.
- Do not remove working code during a documentation cleanup.
- Prefer logical commits by phase.
- Keep every change reviewable.
- Before deleting or moving files, search references first.
- After moves, repair all relative Markdown links, scripts, CI references, tool references, and agent instructions.

### 2.6 Avoid documentation duplication

Do not solve confusion by creating five files that all explain the same thing.

Each important document must have one clear responsibility.

If an existing document already performs that role well, improve it instead of creating a second source of truth.

---

## 3. Existing repository reality that must be respected

The cleanup agent must begin from the repository that actually exists, not from an imagined blank structure.

Known major documentation already includes items such as:

- root `README.md`
- `HERON_AI_MASTER_ARCHITECTURE.md`
- `HERON_CONSTITUTION.md`
- `CONTRIBUTING.md`
- numbered architecture/specification documents under `docs/`
- `docs/README.md`
- `docs/HANDOVER.md`
- `docs/DECISIONS.md`
- `docs/OPEN-QUESTIONS.md`
- `docs/NEEDS-CHECKING.md`
- `docs/FRAGMENT-ISSUES.md`
- `docs/PROPOSALS.md`
- prompt-style Markdown such as `docs/PROMPT-fragment-validation-agent.md`
- module/tool documentation such as `tools/README.md`
- AI-specific structures under `.agents/`, `.claude/`, and `.codex/`

Do not replace this mature documentation system with a generic template.

The job is to **reconcile, simplify, classify, update, and connect it**.

---

# PHASE A — Repository-wide audit

## 4. Build a complete inventory

Audit the entire repository recursively.

Classify every meaningful file and folder into one of the following groups:

| Class | Meaning | Typical action |
|---|---|---|
| **Permanent source** | Production code, tests, configuration, fragments, skills | Keep; document correctly |
| **Permanent authority** | Constitution, accepted specifications, decisions, architecture | Keep; update links/status only |
| **Navigation document** | README, project map, module index | Keep/create/update |
| **Live operational status** | Current gaps, fragment issues, test/proof queue | Keep only if actively maintained and clearly identified |
| **Session/work note** | Handover, temporary planning, current investigation | Prefer `docs/work-notes/`; retain established entry points when migration risk outweighs benefit |
| **One-time execution prompt** | Markdown created to instruct an AI to perform a task | Verify task; delete when finished or keep only while active |
| **Fix note** | Temporary instructions for a specific defect | Verify fix; merge unique knowledge; then delete if no longer needed |
| **Idea/proposal** | Not yet approved work | Move to ideas/proposals area or permanent proposal doc |
| **Historical evidence** | Important explanation of why a decision exists | Preserve in decisions/research/architecture; do not casually delete |
| **Generated/reproducible output** | Can be recreated from tools | Usually do not maintain manually |
| **Obsolete/duplicate** | No unique information and no live references | Delete after verification |

Create the classification from evidence, not filename assumptions.

---

## 5. Audit every Markdown file

Every `.md` file must be checked.

For each Markdown file answer:

1. What is its purpose?
2. Is it permanent or temporary?
3. Is it authoritative or explanatory?
4. Is its information still correct?
5. Does it contain hard-coded counts that can go stale?
6. Does another file duplicate it?
7. Are its links valid?
8. Is it referenced anywhere else?
9. Does it describe work that is already complete?
10. If complete, does any unique knowledge need to be moved before deleting it?
11. Does its title match its actual role?
12. Should a new AI or developer be expected to read it?

Do not limit this audit to `README.md` files. **All Markdown is in scope.**

---

# PHASE B — Truth and status reconciliation

## 6. Reconcile written status against repository truth

This is a critical phase.

Heron changes quickly. A sentence saying "2 tested" may become wrong after 10 are tested. A count saying `DRAFT` may be wrong after proving sessions. A handover note may describe yesterday's branch state.

Find these mismatches systematically.

### Required checks

- fragment counts and lifecycle statuses;
- `PROVEN` versus `DRAFT` claims;
- real Revit proof claims;
- supported Revit versions;
- test suite counts and current pass/fail reasons;
- agent counts;
- MCP tool counts;
- open-question counts;
- register/gap counts;
- active branch references;
- completed architecture tasks;
- completed fix instructions;
- TODO / FIXME / pending wording;
- "not built", "never tested", "waiting", "blocked", and similar claims;
- documentation claiming a feature is missing when code now exists;
- documentation claiming a feature exists when no implementation can be found.

### Status rule

Where a value can be derived automatically, documentation should prefer wording such as:

> Current count is derived by `<command/tool>`; do not maintain the number manually here.

A manually typed snapshot is acceptable only when the date and evidence are useful historically and it is clearly labelled as a snapshot.

---

## 7. Fragment-specific reconciliation

Fragments are a major part of Heron, so their documentation must never give a false sense of readiness.

Audit:

- `brain/fragments/*/fragment.yaml`
- proof/evidence files
- `docs/FRAGMENT-ISSUES.md`
- `docs/NEEDS-CHECKING.md`
- fragment-related handover sections
- fragment validation prompts
- fragment tooling and reports

For each fragment-related status statement:

- verify the actual fragment lifecycle value;
- verify whether proof exists;
- verify whether proof is stale after implementation changes;
- verify whether real Revit was used where required;
- verify whether the negative case/refusal case was actually tested where the project requires it;
- update the documentation to match reality.

Never promote a fragment because a Markdown note says it was tested.

---

# PHASE C — Documentation architecture

## 8. Target documentation model

Use a simple hierarchy with clear responsibilities.

Do **not** move every numbered `docs/` file merely to make the tree prettier. Those documents already have many references and form an established documentation system. Move files only when the benefit is real.

The recommended final structure is conceptually:

```text
Heron-AI/
├── README.md
├── AGENTS.md
├── HERON_CONSTITUTION.md
├── HERON_AI_MASTER_ARCHITECTURE.md   # keep only if still authoritative/useful
├── CONTRIBUTING.md
│
├── docs/
│   ├── README.md                     # canonical documentation index
│   ├── PROJECT-MAP.md                # fast map for human + AI orientation
│   ├── 00-...md                      # established permanent documentation
│   ├── 01-...md
│   ├── ...
│   ├── DECISIONS.md
│   ├── OPEN-QUESTIONS.md
│   ├── NEEDS-CHECKING.md             # only if still a live operational register
│   ├── FRAGMENT-ISSUES.md            # only if still a live operational register
│   │
│   └── work-notes/
│       ├── README.md
│       ├── handover/
│       ├── plans/
│       ├── fixes/
│       ├── ideas/
│       └── investigations/
│
├── platform/
│   └── README.md                    # shared contracts; verify existing equivalent
├── revit/
│   └── README.md
├── mcp/
│   └── README.md
├── brain/
│   └── README.md
├── tools/
│   └── README.md
├── tests/
│   └── README.md
├── .agents/
│   └── README.md          # only if useful and not already explained elsewhere
├── .claude/
│   └── README.md          # only if useful and not redundant
└── .codex/
    └── README.md          # only if useful and not redundant
```

This is a **target responsibility model**, not permission to blindly create every file shown above.

If a useful equivalent already exists, update that equivalent instead.

---

## 9. Root `README.md` responsibility

The root README is for a first-time human reader.

It should answer quickly:

- What is Heron AI?
- Who is it for?
- What are the four major parts?
- What is currently usable/proven versus only built?
- What is the safest way to get current status?
- Where should a developer start?
- Where should an AI agent start?
- Where should a BIM modeller/BIM manager start?
- Where is the documentation index?
- Where is the current work/handover information?

Do not turn the root README into the entire project specification.

Long architecture belongs in permanent architecture documents.

---

## 10. Create or update root `AGENTS.md`

Create a root `AGENTS.md` unless a clearly superior project-wide AI instruction file already exists.

Purpose: a cold AI agent should read one file and know how to work safely in Heron.

It should contain:

### Project identity

- Heron AI is a BIM/Revit engineering platform.
- C# owns Revit-facing work.
- Python owns brain/RAG/orchestration-side work where applicable.
- Revit compatibility requirements.

### Mandatory reading order

Keep the list short and purposeful, for example:

1. `AGENTS.md`
2. root `README.md`
3. `HERON_CONSTITUTION.md`
4. `docs/README.md`
5. `docs/PROJECT-MAP.md`
6. the README for the module being changed
7. applicable decisions/specification sections
8. current work note only when the task requires it

### Safety rules

- never guess Revit proof;
- never bypass safety gates;
- never silently widen write permissions;
- never break Revit 2020–latest compatibility without a documented decision;
- never mix external project code/branding into Heron;
- never treat vector/RAG output as higher authority than canonical source files;
- never edit generated/derived status by hand when a tool owns it;
- never delete work notes until the work they describe is verified.

### Working method

- inspect before edit;
- find references;
- make the smallest safe change;
- run relevant gates/tests;
- update documentation when behavior changes;
- update handover/current-work note only when there is unfinished work to hand over;
- leave the repository cleaner than it was found.

### Source-of-truth map

Point the AI to the correct source for:

- architecture;
- constitution/golden rules;
- decisions;
- fragments;
- open questions;
- Revit proof;
- tests;
- current handover;
- work notes.

Do not duplicate the full content of those files inside `AGENTS.md`.

---

## 11. Create `docs/PROJECT-MAP.md`

This should be the fastest technical orientation document in the repository.

It is for humans **and** AI.

It should include:

### A. System map

Explain the request flow at a high level, for example:

```text
User / AI Host
   ↓
Heron MCP / orchestration
   ↓
Heron Brain / capabilities / skills / fragments
   ↓
Named-pipe bridge
   ↓
Heron Revit add-in
   ↓
ExternalEvent / Revit main thread
   ↓
Revit model
```

Adjust this diagram to the actual current architecture.

### B. Folder map

For each important top-level folder:

- what it owns;
- what it must not own;
- main entry points;
- related documentation;
- related tests;
- important tools/gates.

### C. Change map

Examples:

- "I need to change Revit execution" → where to start.
- "I need to add/update a fragment" → where to start.
- "I need to update an MCP tool" → where to start.
- "I need to change AI behavior" → where to start.
- "I need to understand why an architecture choice exists" → `DECISIONS.md` / specification.
- "I need to continue unfinished work" → current work-note/handover area.

### D. Truth hierarchy

State which source wins when documents disagree.

Separate **required behavior** from **observed behavior**:

- Binding constitution, Golden Rules and accepted decisions govern what Heron is allowed to do. A conflicting implementation is a defect; running code cannot silently amend policy.
- Code, metadata, tool output and fresh proof describe what is implemented or observed at a named revision and environment. They can correct an outdated status sentence, but a passing checker has only its implemented coverage.
- Specifications and permanent architecture explain intent under those decisions. Historical specifications retain their historical wording; point to superseding decisions instead of rewriting history.
- Registers describe current work; temporary notes and old snapshots provide context. Retrieval/vector indexes are derived views, never canonical authority.

Record each contradiction with both sources, the governing decision, the observed evidence, and the owner of its resolution. Leave unresolved policy conflicts open instead of selecting whichever source makes cleanup easiest.

---

# PHASE D — Work-notes separation

## 12. Create `docs/work-notes/`

The owner needs one place to look for temporary or active work without mixing it into permanent product documentation.

Create only populated, justified areas from this model; reuse existing folders:

```text
docs/work-notes/
├── README.md
├── handover/
├── plans/
├── fixes/
├── ideas/
└── investigations/
```

Only add another subfolder if the repository genuinely needs it.

### `docs/work-notes/README.md`

This file must explain:

- this folder is operational workspace, not product specification;
- permanent architecture/decisions belong elsewhere;
- active handover notes go under `handover/`;
- one-time implementation plans/prompts go under `plans/`;
- temporary defect instructions go under `fixes/`;
- unapproved ideas go under `ideas/`;
- research/debug investigation notes go under `investigations/`;
- completed disposable notes are removed after their knowledge is captured;
- historically valuable reasoning is promoted into permanent docs rather than left here forever.

This lets Ajmal inspect one folder when asking, "What work is currently going on?"

---

## 13. Handover notes

`docs/HANDOVER.md` currently acts as a session continuation/memory document. That is operational information, not something every first-time reader needs to study in full.

Audit it carefully.

If its role is still session handover/current-work memory, move or transition it into:

```text
docs/work-notes/handover/HANDOVER.md
```

However:

- do not move it until all inbound links are known;
- update every link after moving;
- update root README / docs README / AGENTS.md so a continuing agent can still find it immediately;
- preserve any unique permanent design lessons by moving those lessons to decisions, architecture, field notes, or other permanent documents first;
- do **not** confuse the session handover file with `docs/00c-master-handover-baseline.md`, which may be an authoritative specification/baseline and must be classified by content, not by the word "handover".

If moving `HANDOVER.md` causes more harm than value because it is already a strongly established canonical entry point, keeping it in place is acceptable — but document clearly that it is an operational/current-work entry point.

The goal is responsibility separation, not cosmetic movement.

---

## 14. One-time prompt files

Audit all prompt-style `.md` files, including files such as:

- `docs/PROMPT-fragment-validation-agent.md`
- architecture-build prompts;
- fix prompts;
- implementation instruction files;
- temporary review prompts.

For each prompt:

### If the instructed work is not complete

Classify it as active or blocked. Move it only through the migration procedure in section 28.4; otherwise retain the established path with an explicit operational role.

### If the instructed work is complete

1. Verify the implementation.
2. Verify tests/proofs where required.
3. Extract any unique knowledge into permanent docs.
4. Update current status documents.
5. Remove stale references to the prompt.
6. Delete the prompt.

A completed one-time prompt should not remain in the repository forever unless it has genuine permanent educational value.

---

## 15. Fix notes

For every temporary fix note:

- verify the defect still exists or is fixed;
- verify the fix in code;
- verify required tests/proofs;
- record the lasting lesson in the correct permanent location if useful;
- remove the completed fix note.

Do not leave documents saying "fix this" after the fix is already merged.

---

## 16. Ideas

Unapproved future ideas should not be mixed with accepted architecture.

Use `docs/work-notes/ideas/` for raw ideas that still need discussion.

Once an idea is accepted:

- record the decision in the project's decision system;
- update architecture/specification/roadmap as needed;
- remove or close the temporary idea note.

Once rejected:

- preserve the rejection reason in `DECISIONS.md` if it is important enough to prevent repeated proposals;
- remove the raw temporary note when safe.

---

# PHASE E — Module-level onboarding

## 17. README coverage for major modules

A new developer or AI should not have to reverse-engineer every directory.

Audit major folders and ensure each **significant subsystem boundary** has a useful README or equivalent documentation.

Likely candidates include:

- `platform/`
- `revit/`
- `mcp/`
- `brain/`
- `tools/`
- `tests/`
- `.agents/`
- `.claude/`
- `.codex/`

Do **not** add a README to every tiny directory.

For example, hundreds of individual fragment folders do not need hundreds of repeated READMEs if their metadata and central fragment documentation already explain them.

### Each major module README should answer

1. What is this folder responsible for?
2. What is outside its responsibility?
3. What are the important entry-point files?
4. What is the normal data/control flow?
5. What are the key constraints?
6. What tests validate it?
7. What tools/gates should be run after modifying it?
8. What documentation should be read before changing it?
9. What Revit versions/platform constraints apply?
10. What common mistakes must be avoided?

Keep these READMEs concise enough to remain useful.

---

# PHASE F — AI-first and human-first navigation

## 18. Define clear entry paths by role

`docs/README.md` or `docs/PROJECT-MAP.md` should provide short role-based routes.

### BIM modeller / BIM coordinator / BIM manager

They should quickly learn:

- what Heron does in BIM/Revit terms;
- what is safe/proven today;
- what requires confirmation;
- how to understand read versus write behavior;
- where user-facing workflow documentation lives.

They should not be forced to understand every Python class or agent framework first.

### Developer

They should quickly learn:

- architecture;
- folder ownership;
- Revit/API rules;
- build/test commands;
- decisions;
- compatibility constraints.

### AI agent

It should quickly learn:

- reading order;
- project rules;
- source-of-truth hierarchy;
- current task location;
- module ownership;
- required validation before claiming success.

### Project owner

Ajmal should be able to answer these questions by opening very few files:

- What is Heron now?
- What is working?
- What is not proven?
- What is being worked on now?
- What is waiting?
- What ideas are still open?
- Where should I tell a new AI to start?

---

# PHASE G — Cross-link and stale-reference cleanup

## 19. Repair the documentation graph

After moving/deleting/renaming anything:

- search every old path;
- repair Markdown links;
- repair source-code comments if they reference moved docs;
- repair agent instructions;
- repair scripts/tools that load named docs;
- repair tests that expect paths;
- repair GitHub issue/PR templates if applicable;
- repair README navigation;
- repair handover references;
- repair prompt references.

Then run the repository's documentation/link checks.

No broken path should be knowingly left behind.

---

## 20. Remove stale statements

Search for phrases commonly associated with stale status:

- `as of`
- `currently`
- `today`
- `never`
- `not built`
- `not tested`
- `unproven`
- `waiting`
- `blocked`
- `pending`
- `TODO`
- `FIXME`
- `DRAFT`
- exact hard-coded counts
- old branch names
- old tool counts
- old test counts

Do not automatically remove these words. Verify whether each statement is still true.

---

# PHASE H — Permanent documentation quality

## 21. One responsibility per important document

After cleanup, every major document should have a clear job.

A suggested responsibility map:

| Document | Responsibility |
|---|---|
| `README.md` | First project introduction and fast start |
| `AGENTS.md` | Rules and reading order for AI agents |
| `docs/README.md` | Full documentation index |
| `docs/PROJECT-MAP.md` | Technical navigation and subsystem map |
| `HERON_CONSTITUTION.md` | Binding project constitution |
| `docs/DECISIONS.md` | Accepted/rejected decisions and reasoning |
| `docs/OPEN-QUESTIONS.md` | Questions genuinely still open |
| numbered `docs/*` | Permanent specification/architecture/research by topic |
| `docs/work-notes/` | Temporary operational work |
| module `README.md` | Local subsystem orientation |

Do not create competing "master" files unless each has a genuinely different responsibility.

---

## 22. Review `HERON_AI_MASTER_ARCHITECTURE.md`

This file is large and important.

Determine whether it is:

- an authoritative permanent architecture document;
- a historical architecture proposal;
- a one-time execution prompt that has now been reconciled;
- partly authoritative and partly superseded.

There is already repository documentation specifically about reconciling master architecture. Use that evidence.

Do not delete this file merely because an architecture reconciliation exists.

Instead make its current authority unmistakable:

- authoritative;
- reference only;
- superseded in part;
- or replaceable after its durable content is merged elsewhere.

A new AI must never mistake an old architecture proposal for current production truth.

---

# PHASE I — Validation and QA

## 23. Run all relevant repository checks

Use the repository's actual available tools.

At minimum, inspect and run applicable checks for:

- documentation consistency;
- links;
- gaps;
- structure;
- metadata;
- licensing;
- agent counts/registry consistency;
- fragment metadata/status;
- Python tests;
- C# compile gate across supported Revit versions where the environment allows;
- MCP tests where dependencies exist.

Do not fake a green result when the environment cannot run a check.

Record clearly:

- PASS;
- FAIL;
- NOT RUN — environment limitation;
- NEEDS REAL REVIT.

Those four states must not be mixed.

---

## 24. Real Revit verification rule

Any cleanup that changes only Markdown does not require inventing new Revit proof.

But if the housekeeping operation reveals that code was changed as part of finishing a fix, then Revit-dependent claims must follow the existing Heron proof rules.

Do not mark a behavior proven because:

- C# compiled;
- Python tests passed;
- an AI says the logic looks correct;
- an old handover note says it worked.

Use actual project evidence.

---

# PHASE J — Final repository audit

## 25. Final checklist

Apply the evidence and extended closure checklist in sections 28.6–28.10 as part of this audit.

Before declaring the housekeeping finished, verify all of the following.

### Structure

- [ ] Major folders have clear responsibilities.
- [ ] Permanent docs and work notes are clearly separated.
- [ ] Temporary plans/fixes/prompts are not scattered through permanent docs.
- [ ] No unnecessary empty folders remain.
- [ ] No pointless README proliferation was introduced.

### Documentation

- [ ] Root README reflects current project reality.
- [ ] `docs/README.md` accurately indexes permanent documentation.
- [ ] AI entry instructions exist and are current.
- [ ] Project/folder map exists or an equivalent provides the same function.
- [ ] Major module READMEs are present where genuinely useful.
- [ ] No two files claim to be the same source of truth.

### Status

- [ ] Fragment status statements match repository evidence.
- [ ] Tested/proven wording is correct.
- [ ] Counts are derived where practical.
- [ ] Completed work is no longer listed as pending.
- [ ] Pending work is not falsely shown as complete.

### Cleanup

- [ ] Completed one-time prompts have disposition records: deleted after verification or retained with a specific preservation reason.
- [ ] Completed fix notes were retired after durable knowledge was captured, or retained with a justified evidence role.
- [ ] Duplicate/outdated files were removed safely.
- [ ] Important historical decisions were preserved.
- [ ] No unique technical lesson was lost during cleanup.

### References

- [ ] No broken Markdown links.
- [ ] No references to deleted files.
- [ ] No code/tool path references broken by moves.
- [ ] No agent instruction references broken by moves.

### Validation

- [ ] Documentation checks pass where available.
- [ ] Structure and metadata gates pass; gap findings are classified, with outstanding product proof distinguished from housekeeping regressions.
- [ ] Tests were run where the environment permits.
- [ ] Revit-only items are clearly labelled as Revit-only rather than guessed.

### Git

- [ ] Changes are logically committed.
- [ ] No unrelated files were modified.
- [ ] No generated junk is committed.
- [ ] Owned worktree and staged paths are understood; unrelated concurrent changes are listed, preserved and excluded.
- [ ] Remote branch SHA and draft PR are verified; default-branch integration is reported separately.

---

# PHASE K — Final handover after cleanup

## 26. Produce a short final report

Before deleting this plan, create/update the correct permanent or operational record with a short summary containing:

- what was reorganized;
- what was deleted;
- what was moved;
- what documentation was created;
- what stale status was corrected;
- which checks passed;
- which checks could not run;
- what still requires Revit;
- any remaining open housekeeping issue.

Do not create another permanent giant cleanup report unless there is a real reason to keep it.

The final report can live in the current handover/work-note system if it is only operational.

---

# PHASE L — Self-removal rule

## 27. Delete this file last

This file exists only to drive the housekeeping operation.

Path:

```text
docs/work-notes/plans/repository-housekeeping-and-ai-onboarding-plan.md
```

Delete it **only when**:

1. every applicable phase above has been executed;
2. all completed temporary prompts/fix notes have been handled;
3. documentation status matches repository truth;
4. required links/references have been repaired;
5. every required housekeeping gate passes; optional/environment-dependent checks have explicit scope decisions and owners, not a blanket waiver;
6. the final repository structure is understandable without this plan;
7. any remaining work is recorded in the correct live work-note/handover location.

If required housekeeping work remains, **do not delete this file**. Update its checklist or handover state instead. Unrelated product proof can remain open in its existing register. Optional work may be deferred with rationale and an owner; a broken migration or missing required verification cannot be reclassified as optional to close this plan. Preserve the disposition ledger and closure evidence outside this file before removing it, then check references again.

---

# 28. Practical execution controls and task cards

Read this section **before Phase A**. It supplies the operating procedure for phases A–L, not a second housekeeping project. Checkboxes above remain unchecked until execution evidence exists.

## 28.1 Priorities, scope and baseline

**Must do:** inventory all in-scope repository files; resolve misleading current authority/status; preserve unique knowledge; repair every affected reference; provide usable entry routes; verify changed paths and record closure. **Optional:** cosmetic renames, splitting a large document, new diagrams, extra indexes and folder reshaping without a concrete navigation benefit. Assess every significant folder, but a documented keep-in-place decision is a valid result. Do not turn the registry into a mandate to build unimplemented agents.

The four product boundaries are `platform/`, `revit/`, `brain/`, and `mcp/`. Include `tools/`, `tests/`, `docs/`, root configuration, hidden instruction/configuration folders and `.github/` in the map. The conceptual tree in section 8 is not a complete file inventory. Verify all paths at execution time.

Begin from the repository root in PowerShell. These are inspection commands, not cleanup commands:

```powershell
git status --short
git branch --show-current
git rev-parse HEAD
git remote -v
git worktree list
git diff --name-status
git diff --cached --name-status
git ls-files
git ls-files --others --exclude-standard
rg --files --hidden -g '!.git' -g '!**/bin/**' -g '!**/obj/**'
```

Record date/time, commit, branch, upstream, dirty paths, active owners, tool versions and available Python/.NET/Revit/MCP dependencies in a single working ledger. Use `docs/work-notes/plans/housekeeping-execution-record.md` only if no existing execution record already owns this work; it is a proposed deliverable, not an existing file. Keep verbose logs in a local task output folder and commit only sanitized evidence necessary for review.

Inventory tracked and untracked files separately. Enumerate ignored paths using Git ignore information only when needed to explain exclusions; do not ingest private knowledge stores or model contents into the ledger. Group reproducible `bin/`, `obj/`, caches and package contents by generator with counts/ignore reason instead of reading every generated byte. Read all repository-owned Markdown, including hidden folders, and every source/configuration file proposed for alteration. Record unreadable files as unresolved. Reconcile inventory totals against tracked-file output; record additions/removals since baseline before closure.

For each baseline check retain command, exit code, meaningful findings, environment and commit. A pre-existing failure is not automatically caused by this change; compare before/after. Conversely, a failure mentioned in an old skill is not automatically harmless on this machine.

## 28.2 Phase dependencies, owners and entry/exit gates

Assign one execution owner and one reviewer for the overall batch; assign a module maintainer role for each affected area. These are responsibilities, not invented Heron agent IDs. The executor maintains evidence; the reviewer checks it; Ajmal resolves product intent or unresolved authority decisions. In a single-agent run, mark independent review pending rather than pretending self-review is independent approval.

| Phase | Entry / dependency | Concrete work and output | Exit evidence |
|---|---|---|---|
| A | Authorized execution; safe checkout and ownership recorded | Inventory/classification ledger, exclusions, baseline checks, conflict list | Every in-scope file accounted for; no move/delete candidate unread |
| B | A inventory and baseline | Compare current claims to tools, code and proof; record source-to-claim corrections | Each changed claim cites evidence; unresolved conflicts have owner and next action |
| C | A; B authority findings | Draft responsibility map; decide existing versus new entry files; justify proposed moves | One canonical owner per topic and reviewed migration map |
| D | C map and per-file disposition | Preserve knowledge, relocate or retain operational notes in small batches | Each batch passes reference checks before commit; durable destination verified |
| E | C boundaries; D final paths for affected modules | Improve significant module READMEs, including platform contracts | Entry points, constraints and exact relevant checks verified against source |
| F | B current claims; C/E entry documents | Add role routes and definitions; perform section 28.7 tasks | Reader can reach authoritative answers; failures recorded and repaired |
| G | Every move/delete batch, then D–F | Repair links, anchors, code/config/tool paths and generated navigation | No unresolved affected live reference; external consumers addressed |
| H | B–G coherent | Reconcile master architecture with accepted decisions; remove duplication | Authority and lifecycle readable; historical content preserved |
| I | Changed batch ready; repeat for final snapshot | Run section 28.6 matrix and fresh-checkout walkthrough | Required checks pass; baseline defects and unrun checks named |
| J | A–I outputs | Reconcile final inventory, disposition coverage and section 25 checklist | Required tasks complete; no unexplained omissions or broken migration |
| K | J accepted | Short closure report, maintenance ownership, durable ledger/evidence location | Another session can continue without chat memory |
| L | K record survives independently | Remove this temporary plan only after its closure conditions; repair inbound links | Final checks rerun after removal, reviewable commit and remote evidence |

G is a continuous gate, not permission to leave broken links until late in the project. Independent documentation drafting may overlap, but do not edit the same file concurrently. A phase can be partially complete; report which task remains blocked. Never mark all of housekeeping done because one batch passed.

## 28.3 Disposition, knowledge and uncertain-file records

Use one ledger row per affected file, plus grouped keep records for unchanged source where appropriate. Required fields:

| Field | Required content |
|---|---|
| Identity | Original repo-relative path; baseline commit; content hash where needed |
| Role and ownership | Class, canonical topic owner, maintainer/reviewer, active session if any |
| Decision | Keep / update / move / merge / delete / defer; reason and priority |
| Evidence | Read completed; inbound/outbound references; task/fix status; proof freshness |
| Knowledge destination | Exact permanent path and heading; what unique lesson/decision is retained |
| Migration | Old-to-new path and anchors; affected loaders/tests/configuration; compatibility handling |
| Completion | Verification command/result, reviewer, commit, next review trigger or remaining blocker |

For merge/delete, first summarize the unique constraints, failed approaches and decision rationale. Add them to the existing authoritative destination without changing their meaning. Compare source and destination side by side; record what was intentionally omitted and why. Repair references and only then remove the disposable file. Git history is recovery evidence, not a substitute for discoverable current knowledge.

Lifecycle for operational notes: **active → blocked or completed → knowledge preserved → retired**. A retained historical record is labelled historical and links to the current owner; it does not keep issuing active instructions. Put status, owner role, last reviewed date and closure condition in the note or central index, avoiding duplicate metadata everywhere.

If ownership, uniqueness, reference coverage or completion is uncertain, choose **defer/keep**. Record the precise question, evidence already inspected, who can answer, next action and review trigger in the existing open-work system. Do not invent a deadline or delete after a timeout. An unresolved optional tidy-up can be deferred; an unresolved dependency of a planned deletion blocks that deletion. Existing decision IDs remain stable; do not renumber decisions or Golden Rules during housekeeping.

## 28.4 Safe folder reorganization and reference repair

Folder refactoring is in scope for the future execution, where justified. Prefer one bounded move with all consumers repaired over a repository-wide rename. Keep the four product boundaries and existing project identities. Moving source projects, fragments or runtime assets is higher risk than moving prose and requires their module-specific checks.

Before each move:

- [ ] Explain the practical benefit, affected readers/loaders, and why an index/link improvement alone is insufficient.
- [ ] Record old path → new path, old heading → new heading, owner, batch, rollback method and checks. Include case-only changes and filename collisions on Windows.
- [ ] Inspect full path, basename, relative path variants, forward/backslashes and dynamic path construction. Search tracked and hidden repository files with `rg -n --hidden -F -g '!.git' 'old-path' .`; replace the literal old-path with the actual candidate. A zero literal hit does not prove absence of runtime consumers.
- [ ] Inspect Markdown links/anchors, reference-style links, HTML links, image sources, README indexes, prompts, skills, `.mcp.json`, host configuration, CI/templates when present, project references, imports, working-directory assumptions, package data and tests.
- [ ] Check external entry points: saved prompts, installed add-in/configuration paths, GitHub links and other worktrees. If a consumer cannot be updated, retain the old entry point with an explicit compatibility strategy or defer the move.
- [ ] Move tracked files with `git mv` using literal quoted paths, then repair inbound **and outbound** links in the same batch. Relative links inside the moved document often change even when their destination did not move.
- [ ] Rerun searches and selected validation; inspect `git diff --summary` and `git diff --check`. Review GitHub-style anchors and case-sensitive path spelling; Windows file existence alone misses case defects.
- [ ] Regenerate affected derived catalogs/indexes using their documented generator, inspect results, and commit only outputs whose repository policy requires tracking. Do not hand-edit vector databases or generated counts.

A compatibility stub must contain only the current destination and its migration purpose; it must not become a second copy of authoritative content. Give it a removal trigger based on consumer migration, not age alone. Historical mentions of old paths may remain labelled as history; every live dependency must resolve. For runtime paths, test the actual loader from a fresh checkout and a working directory containing spaces.

**Fragment boundary:** `brain/heron_fragment.py` fingerprints implementation paths as well as normalized content. A path-only move can affect proof identity. Compare metadata/validator results before and after, preserve original evidence, and use the established proof workflow if new proof is needed. Never restamp or promote merely to make a housekeeping gate green.

## 28.5 Concurrent sessions and interruption recovery

Use a dedicated `codex/` branch and isolated worktree when other sessions are active. Before claiming a file, inspect working-tree changes and current tasks/worktrees; coordinate overlapping ownership. Never overwrite another session's changes, stash their work, clean their untracked files or stage the entire repository. Stage explicit owned paths and review the staged diff. Do not restart Revit, deploy binaries or run shared-output build/proof tools while another session is using them without coordination.

Checkpoint after each coherent batch: baseline and current commit, owned paths, disposition rows completed, checks and findings, pending moves, next exact action, and any necessary environment assumptions. Never store tokens or private model identifiers in a public-ready checkpoint.

On interruption, reread the checkpoint and current Git state, compare the plan/file hashes and remote branch, then inspect partial moves before resuming. Re-run checks invalidated by intervening edits. If a batch must be undone, reverse only owned changes; use a normal revert for an already committed batch, preserving later work. Never use blanket reset/clean or history rewriting. If a newer session already completed a task, verify and reuse that result rather than publishing a duplicate change.

## 28.6 Validation matrix and evidence interpretation

Read the current `tools/README.md`, check implementation and applicable shipping instructions at execution time. Commands below exist in the inspected repository; their outputs and coverage may evolve. Run from the repository root. This matrix selects checks; it does not authorize deploying or changing a live model.

| Change or claim | Actual command / inspection | Acceptance and limitation |
|---|---|---|
| Any documentation batch | `python tools/check-docs.py` | Exit status **and** broken-link/reference/count output reviewed; no introduced defects. Link findings are not all fatal in the current implementation |
| Any batch | `python tools/check-metadata.py`; `python tools/check-structure.py`; `git diff --check` | No new header, registry, layering or whitespace defects; required gates pass |
| Repository status | `python tools/check-gaps.py`; `python tools/agent-count.py` | Read findings; current gaps exit code follows UNFINISHED, while WAITING is reported separately and remains unproven. Agent reconciliation failures require investigation |
| Fragment status/path changes | `python brain/heron_fragment.py` | Metadata and freshness findings classified; no unearned lifecycle change |
| Preservation/licensing | `python tools/check-licence.py` plus manual `LICENSE`, `NOTICE`, `CONTRIBUTING.md` review | Retain attribution and notices; automated coverage is not legal clearance |
| Reachability/behavior claims | `python tools/check-reachable.py`; `python tools/check-revit-gate.py` | Reports guide questions; exit zero alone does not mean every finding is resolved |
| Python/MCP path changes; shipping regression suite | Run each `tests/test_*.py` as below | Record each result; investigate actual errors rather than inheriting old environment failure lists |
| C# project/build/path changes | `python tools/check-compile.py` | Record each requested release and skips; needs suitable SDK/reference access; shared outputs can overwrite deployed-version build artifacts |
| Fragment implementation/path changes | `python tools/check-fragments-compile.py` | Requires compiler/dependencies; declared supported releases covered or explicitly blocked |
| Routing/index changes | Inspect and run `check-routing.py` / `check-intrusion.py` as documented | Use a controlled knowledge store, preserve scope isolation; record data/environment dependencies |
| Revit-dependent behavior changed | Existing fragment validation/proving workflow and appropriate live model | Named environment, actual positive/negative results, freshness and rollback/apply semantics; compile is insufficient |

PowerShell regression loop, preserving exit codes and printing each suite result:

```powershell
$housekeepingFailures = @()
Get-ChildItem tests/test_*.py | Sort-Object Name | ForEach-Object {
    python $_.FullName
    $suiteExit = $LASTEXITCODE
    Write-Output "$($_.Name): exit $suiteExit"
    if ($suiteExit -ne 0) { $housekeepingFailures += $_.Name }
}
$housekeepingFailures
```

Do not invent a universal all-checks command. Some tools mutate files: for example, `recount-agent-registry.py` rewrites counts. Inspect before running generators or repair modes. A Markdown-only revision needs no new live Revit proof or compiler run unless its actual effect reaches executable configuration.

Each evidence entry must include scope/path, command, commit, timestamp, environment/dependencies, exit code, relevant output, verdict and remaining action. Use PASS, FAIL, NOT RUN (with reason), or NEEDS REAL REVIT. Add a separate relevance field: required / optional / not applicable. An unrelated product gap is open work, not a housekeeping pass or failure. A baseline link defect may be outside a plan-only revision, but required final repository link cleanup cannot be declared complete while that defect remains.

Proof freshness depends on what was tested: implementation fingerprint, executor/dependency changes, Revit release and model/context. An unchanged fragment fingerprint does not prove a changed executor still behaves correctly. Retain historical proof and mark its limits; re-run the affected proof when required. Never update historical dates to imply a fresh run.

## 28.7 Role-based onboarding acceptance and fresh checkout

Use the final entry documents without this plan or conversation history. Record route taken, answer source, obstacles and result for each task. Aim for the first useful answer within a few documents; measure actual time and document count rather than claiming an arbitrary perfect score.

| Reader | Practical acceptance task | Pass evidence |
|---|---|---|
| Ajmal / owner | Find current capability status, unfinished work and the place to start a new AI | Can distinguish built, deployed and proven; names current sources rather than stale totals |
| BIM modeller | Find a read versus write workflow and how to identify unproven behavior | Explains model-impact boundary and proof limits in BIM terms |
| BIM manager | Find compatibility, safety/approval and data-scope rules | Locates binding rules and distinguishes declared support from tested releases |
| Developer | Trace MCP → bridge → Revit entry path and locate the relevant checks | Identifies actual files, platform contracts, threading/version constraints and executable test command |
| Fresh AI | Find governing instructions, task owner, applicable module docs and validation requirements | Produces a small safe change route without inventing agents, executing stale prompts or treating retrieval as authority |

Add concise glossary guidance in an existing navigation document, not necessarily a new glossary file. Define fragment (reusable implementation unit), skill (user-facing capability), host (AI application providing orchestration), MCP (tool interface), bridge (request transport into Revit), ExternalEvent (safe scheduling onto the Revit main thread), lifecycle, proof, adapter and canonical source. Link to existing detailed definitions; do not redefine lifecycle states or hide uncertainty behind jargon.

Fresh-checkout walkthrough at each candidate final commit (repeat affected checks after any repair or final plan removal):

1. Use a new clean clone/worktree at the exact review commit; record it. Do not borrow untracked modules, local indexes or cached generated navigation from the author's checkout.
2. Follow root README and AI entry instructions as written. Record missing prerequisites rather than silently using the existing machine's setup.
3. Follow each role route, verify case-correct links/anchors and locate the current work record. Exercise paths with spaces; inspect Linux/case-sensitive behavior where available and state when not tested.
4. Run documentation, metadata and structure checks, then applicable test/loader commands from the matrix. Confirm generated artifacts can be recreated if the documented workflow depends on them.
5. Reconcile results against the original worktree. Repair missing tracked dependencies or hidden path assumptions, then repeat only the affected walkthrough steps.
6. Verify staged/committed file list and remote branch SHA. Open a draft PR on the session branch under current repository policy. Report push, PR creation and merge as separate outcomes; do not claim main changed until verified.

## 28.8 Preservation and privacy boundaries

Keep accepted rules, stable IDs, historical master specification wording, proof records, licences/attributions, test fixtures and safety/version constraints intact. A proposed behavior fix discovered during cleanup is a separate implementation task unless specifically authorized; record the defect instead of expanding housekeeping silently.

Inspect the owned diff for credentials, client/company/project data, real model names, user-specific absolute paths, machine usernames, local URLs, logs and exported knowledge. Use repository-relative paths in durable docs; show environment variables or clearly labelled synthetic examples when a local installation path is essential. Follow `CONTRIBUTING.md`, `.gitignore` and `SECURITY.md`. Do not copy sensitive findings into the execution ledger or PR; keep only a sanitized reference and use the private reporting route if required. Ignore rules do not prove that tracked content is safe.

## 28.9 Worked disposition examples

**Established handover:** Inspect `docs/HANDOVER.md`, its inbound references and current consumers. If moving it would break saved continuation prompts, keep the path as an operational entry point and extract durable lessons into the existing decisions/field-note destination. If a move is justified, map the new path and every internal relative link; retain a short old-path pointer until consumers migrate. Evidence includes old-path search results, repaired links and a cold continuation test. A heading containing "handover" does not make `docs/00c-master-handover-baseline.md` disposable.

**Completed fix prompt:** For `docs/PROMPT-fragment-validation-agent.md`, treat completion as a question. Match each requested deliverable to implementation, tests and required proof. If any required item is unproven, retain the prompt with an explicit remaining item. If complete, preserve unique validation lessons under the existing fragment documentation or decision, record destination headings, repair references and retire it. This example does not assert that the prompt is complete today.

**Unknown file:** A generated-looking catalog with no documented generator is kept/deferred. Find the producer and consumers, demonstrate regeneration in a scratch output location, compare meaningful content and check tracking policy. Only then decide whether it is reproducible output. A filename or ignore pattern alone is insufficient deletion evidence.

**Stale proof after reorganization:** A fragment implementation is moved without text changes. Compare validator output and fingerprint before/after, identify path participation, and retain the original proof. If the prescribed workflow cannot establish validity without a new model run, defer the move or schedule the required proof. Do not replace the proof hash by hand.

## 28.10 Completion evidence and continuing maintenance

Add these to section 25's final audit:

- [ ] Baseline/final inventories reconcile, including exclusions and concurrent additions.
- [ ] Every move, merge, delete and defer has a disposition reason and owner.
- [ ] Unique knowledge destinations are verified; immutable history and licensing are preserved.
- [ ] Each affected old path/anchor has verified consumers or a documented compatibility entry point.
- [ ] Role tasks and fresh-checkout walkthrough have actual results and limitations.
- [ ] Privacy and machine-specific path review covers the exact staged content.
- [ ] Required failures remain blocking; optional deferrals have owners and triggers.
- [ ] Durable closure record survives plan removal and links to review/commit evidence.

Keep maintenance proportional: on a behavior/path/status change, the affected module owner updates its canonical page and derived reports; on a work-note completion, the task owner performs disposition review; before a release or major onboarding handover, the reviewer repeats navigation/link and stale-claim checks. Review blocked/deferred notes when their dependency changes. Reuse existing CI or shipping checks; propose missing enforcement separately instead of creating an unsolicited automation. Record these responsibilities in the existing contributor/work-note guide so they survive this plan.

A successful cleanup is a verifiable improvement with clearly bounded remaining product work. It is not a promise that every future reader, machine, external link or Revit model has been tested.

---

# EXECUTION PROMPT — USE ONLY WHEN READY TO RUN THE HOUSEKEEPING

> You are the repository housekeeping, documentation, and onboarding agent for **Heron AI**.
>
> Activate this prompt only when the user authorizes executing housekeeping, not when asked to revise or publish the plan. Work in the authorized `Ajmalpshaik/Heron-AI` checkout, using an isolated session branch when appropriate. Read section 28 first, then execute phases A–L through their entry/exit gates. Record ownership, baseline, disposition decisions and interruption checkpoints.
>
> First inspect the complete repository recursively. Read and classify the files before changing them. Pay special attention to every `.md` file, all current status documents, fragment proof/status data, architecture documents, work instructions, temporary prompts, fix notes, READMEs, agent instructions, tests, tools, and cross-references.
>
> Reconcile every status claim against the repository's real evidence. Prefer machine-derived truth from Heron's own tools, metadata, tests, fragment lifecycle files, compile gates, and real Revit proof records over manually typed Markdown counts. Never mark something tested, proven, complete, supported, or closed without evidence.
>
> Restructure documentation carefully so permanent architecture/specifications/decisions stay separate from temporary operational work. Establish a clean `docs/work-notes/` system for handover notes, plans, fixes, ideas, and investigations where appropriate. Do not move files simply for cosmetic reasons. Preserve the existing numbered documentation system when it is already useful.
>
> Make the repository easy to understand for four audiences: Ajmal as project owner/BIM modeller, a BIM manager or modeller joining the project, a developer, and a fresh AI agent. A new AI must be able to enter cold, read a small number of entry documents, understand the project map, know the source-of-truth hierarchy, find the current task, and work safely without reading the entire repository first.
>
> Create or improve the project-wide AI onboarding file (`AGENTS.md`) and technical navigation (`docs/PROJECT-MAP.md`) only after checking whether equivalent documents already exist. Ensure significant subsystem folders have useful README-level onboarding where needed, but do not create repetitive README files in every small folder.
>
> For every one-time prompt, architecture execution instruction, or fix note: determine whether the requested work is actually finished. If unfinished, retain it or migrate it through the verified mapping into the correct active work-note location. If finished, verify the implementation and required tests/proofs, transfer any unique durable knowledge into permanent documentation, repair references, then delete the obsolete temporary file.
>
> Update **all affected documentation**, not only root `README.md`. If a status changed, find every meaningful place that states the old status and reconcile it. Do not leave "2 tested" in one file when the repository proves 10, and do not maintain fragile hard-coded counts when a reliable command/tool can derive them.
>
> Include justified folder refactoring through section 28.4, with old-to-new mapping, repaired consumers, compatibility handling and rollback. Keep unknown files until the disposition question is resolved. Before any deletion or move, search all references. After changes, repair Markdown links, code comments, script paths, tests, agent instructions, and documentation indexes. Run the repository's available documentation, structure, metadata, gap, licence, test, and compile checks as applicable. Clearly separate PASS, FAIL, NOT RUN, and NEEDS REAL REVIT. Never fake verification because the environment lacks Revit, .NET, MCP dependencies, or another required component.
>
> Keep Heron-specific architecture, naming, safety, compatibility, and project rules intact. Do not dump content from other repositories. Do not simplify away important lessons. Do not rewrite history. Do not force-push. Make logical, reviewable commits and avoid unrelated changes.
>
> Run the role-based tasks and fresh-checkout walkthrough, inspect privacy/machine-specific paths in the exact diff, and distinguish checker exit codes from findings. Never change binding rules to match defective code. Keep required failures blocking and assign optional deferrals an owner. When finished, perform the complete final checklist in sections 25 and 28.10. Leave one short final handover/report in the appropriate live documentation location describing what changed and what, if anything, still needs attention.
>
> **Final action:** only after required housekeeping gates pass, durable disposition/closure evidence survives elsewhere, concurrent work is preserved, and the repository is understandable without this plan, delete `docs/work-notes/plans/repository-housekeeping-and-ai-onboarding-plan.md` itself. This plan must be the last temporary file removed.
