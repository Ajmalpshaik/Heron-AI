# 02 — The plan: eleven packages

> **Type:** Operational work note. **Not specification.** Part of [the earlier-brain plan](README.md).
> **Status:** **Planned 2026-09-23. The table in §0 is the only place the status of a package is kept.**
> **Owner:** Ajmal PS.

---

## 0. Status — the only copy

| Package | Builds | Runs in | Wave | Needs first | State |
|---|---|---|---|---|---|
| **C1** | Guards and checks that run in every session | cloud | 1 | — | **merged** as [#308](https://github.com/Ajmalpshaik/Heron-AI/pull/308), 2026-09-23, on the owner's word - owed on the PC: NEEDS-CHECKING **Group AI**, four checks, no Revit |
| **C2** | The MCP server: a read-only door, rules for every chat, safety labels, the preview decision | cloud | 1 | owner decision **D1** | **merged** in [#307](https://github.com/Ajmalpshaik/Heron-AI/pull/307), on the owner's instruction - D1 answered **B** (keep changing at once), recorded as [D-99](../../../DECISIONS.md#d-99--a-change-asked-for-in-a-chat-is-kept-at-once-with-no-preview-and-article-9-says-so). Still owed: the PC proofs, [NEEDS-CHECKING Group AH](../../../NEEDS-CHECKING.md), and the owner's answer to [FRAGMENT-ISSUES 5b-161](../../../FRAGMENT-ISSUES.md) |
| **C3** | Add-in safety (Revit failure handling, "changed in central?") and an API signature lookup | cloud, then PC | 1 | — | **merged — [#306](https://github.com/Ajmalpshaik/Heron-AI/pull/306)**, 2026-09-23: the cloud part. Compiled on all eight releases, never run in Revit; the PC owes NEEDS-CHECKING **E19–E24** |
| **C4** | Revit traps written into the fragments they affect | cloud, then PC | 1 | — | not started |
| **C5** | The owner's words and standards — knowledge, not language | cloud, then PC | 2 | C2 merged | not started |
| **C6** | A score for the owner's real questions | cloud | 1 | — | **DONE** — PR [#305](https://github.com/Ajmalpshaik/Heron-AI/pull/305). Answer key confirmed by the owner 2026-09-23; **D3 answered: report only** ([D-100](../../../DECISIONS.md)); first score recorded in [`brain/retrieval-history.md`](../../../../brain/retrieval-history.md) |
| **C7** | A fragment-writing agent | cloud | 2 | C3 merged | not started |
| **C8** | Three fragments from the outside-library notes | cloud, then PC | 2 | C4 merged | not started |
| **C9** | Skills that carry a method — the design, then the HVAC methods | cloud, then PC | 3 | owner decision **D2** | not started |
| **C10** | Tagging and sprinkler methods | cloud, then PC | 3 | C9 merged | not started |
| **C11** | Family creation | cloud, then PC | 3, last | C7 and C9 merged | not started |
| — | PR #299, the risk gate on `revit_change` ([01 §3 H1](01-findings.md)) | cloud, then PC | — | **the owner's word to merge** | **merged** as [#299](https://github.com/Ajmalpshaik/Heron-AI/pull/299), 2026-09-22 - FRAGMENT-ISSUES **5b-155** FIXED; owed on the PC: its refusal check in [05](05-pc-proving.md) |

**Wave 1 runs in parallel** — five sessions whose files do not overlap. **Wave 2** starts when the package
it needs has merged. **Wave 3** starts with the owner's answer to D2. Proving on the PC
([05](05-pc-proving.md)) can start package by package, as each one merges.

## 1. Rules every package follows

These are Heron's, not new ones. [`04-cloud-prompts.md` §0](04-cloud-prompts.md) turns them into the
instructions a session reads first.

- **Nothing breaks.** A package lists what it connects to and changes those in the same pull request —
  a new tool gets its registry row, its test, its README row and every document that counts tools.
- **Prove it the Heron way.** A new check is seen to **fail** on the old code before it passes
  ([`heron-ship` §2a](../../../../.claude/skills/heron-ship/SKILL.md)); the ten gates CI runs are green;
  `tools/check-change.py` passes with a `tools/change-evidence.py` record. A green board is not a proof of
  Revit behaviour — that is [D-30](../../../DECISIONS.md) and it happens on the PC ([05](05-pc-proving.md)).
- **A fragment's proof covers its code.** The fingerprint is one hash over the implementation
  (`fingerprint()` in [`brain/heron_fragment.py`](../../../../brain/heron_fragment.py), verified) — so a
  note added to a PROVEN fragment's card keeps its proof, and **any** change under its `impl/` makes the
  proof stale and owes a re-proof on the PC.
- **Re-author, never copy.** No name, path or wording from the earlier library ([README](README.md)).
- **Registers.** Another session is splitting `FRAGMENT-ISSUES`, `NEEDS-CHECKING`, `HANDOVER` and
  `DECISIONS` into smaller files. Until that has merged, do not edit those four: put the rows you owe in
  your pull request under **"Register rows owed"**. Once it has merged, follow the new layout and take the
  next free id at the moment you write.
- **Never merge.** Draft pull requests only; the owner merges. **Never widen write permission**
  (`write.enabled` stays `false`).

## 2. Decisions only the owner can make

Each is asked **once, on its own**, by the package that needs it — never four at a time.

| | The question | Asked by |
|---|---|---|
| **D1** | `revit_change` keeps a change with no preview. Article 9 says a preview first. **Add the preview** (try it, show the counts, keep it only on his yes), **or record the exception** as a decision and amend Article 9? The code and the Constitution may not go on disagreeing | C2 |
| **D2** | Where does a skill's step-by-step method live? **Recommended:** an ordered `steps:` list in the skill itself, each step naming its capability, its inputs and the check before the next step. The alternative is the third "recipe" kind [D-29](../../../DECISIONS.md) named and nothing uses | C9 |
| **D3** | Does the score of his real questions only **report**, or does a drop **fail** the pull request? Heron's routing checks report today | C6 |
| **D4** | The open grayout choices in [03 §C](03-owner-data.md) — service sub-categories, duct linings, conduits, air terminals | C5, one at a time |

## 3. The packages

### C1 — Guards and checks that run in every session

*For developing Heron. Nothing here reaches a model or a modeller.*

1. **Switch the guard on everywhere.** Wire `heron_guard.py` from a committed `.claude/settings.json`
   (PreToolUse, `Write|Edit|MultiEdit`, command
   `python "$CLAUDE_PROJECT_DIR/.claude/skills/heron-guard/bin/heron_guard.py"` — `python` exists on the
   PC and in the cloud, where [`tools/cloud-setup.sh`](../../../../tools/cloud-setup.sh) installs
   `python-is-python3`). Remove the `hooks:` block from the skill's frontmatter so it runs once. Then make
   everything that describes it true: the skill, `tests/test_heron_guard.py` (it asserts the frontmatter
   declares the hook), [34 §2.9](../../../34-patterns-adapted.md) and the answer to Q-49 in
   [OPEN-QUESTIONS](../../../OPEN-QUESTIONS.md) — append a dated line saying why the wiring moved
   ([01 §3 H2](01-findings.md)).
2. **"Has `main` moved?" before a merge.** A PreToolUse hook on `Bash` that, when the command merges or
   marks a pull request ready, fetches `origin/main` (with a timeout) and lists the commits the branch does
   not have, as advice to the AI. **It never blocks.** Why: on 2026-09-22 a whole pull request duplicated
   one merged an hour earlier.
3. **One status line when a session starts** — branch behind or ahead of `origin/main`, proven and draft
   fragment counts (derived), and nothing slow. Silent on any failure. Must work in Git Bash on Windows
   and on Linux.
4. **Hooks that record themselves.** Every hook above appends one line per decision (what it said, when)
   to a local log outside the repository, found through Heron's own path helper rather than a typed path;
   and a small report says how often each one fired and what it said — so a hook that only nags is found
   from evidence.
5. **Three checks, added to `tools/check-docs.py`:**
   - every file in `tools/` is named in `tools/README.md`, and every skill folder in
     `.claude/skills/README.md` — one direction only — **with the 9 missing rows added in the same change**
     (verified 2026-09-22: `HeronRevit.ps1`, `balance-of-work.py`, `build-release-assets.py`,
     `check-products.py`, `cloud-setup.sh`, `deploy-addin.ps1`, `module-reach.py`, `prove-agent.py`,
     `prove-tracking.py`);
   - **text hygiene** over `git ls-files`: control characters other than tab, CR and LF; double-encoded
     text; merge-conflict markers. Build the forbidden bytes with `chr()` — never type them. A hit inside
     a PROVEN fragment's `impl/` is reported as *waiting*, because fixing it breaks the proof. The two
     bytes already in `docs/FRAGMENT-ISSUES.md` are fixed once the register split allows it;
   - **the MCP tool total derived** from `heron_tools.TOOLS` and prose checked against it — fixing the "14
     MCP tools" lines in [33](../../../33-external-repository-research.md) and
     [34](../../../34-patterns-adapted.md) — and replacing the count pattern in `check-docs.py` that today
     matches no line at all (reported).
   - `check-docs` already took 30–37 s on a busy PC on 2026-09-22. **Measure it before and after**, and do
     not make it slower than it needs to be.

**Owns:** `.claude/settings.json`, `.claude/skills/heron-guard/`, a new skill folder for the two new hooks
(with its `bin/`), `.claude/skills/README.md`, `.claude/skills/heron-ship/SKILL.md`, `tools/check-docs.py`,
`tools/README.md`, one new report tool, their tests, and the two lines in 33 and 34.
**Does not touch:** `mcp/`, `revit/`, `brain/`, the four registers.
**Done when:** each hook is unit-tested by piping JSON into it, as the guard's test already does; each check
is seen to fail on a planted fault; the ten gates are green.
**On the PC:** one real Windows session start showing the line, and the guard refusing in a fresh session.

### C2 — The MCP server: a read-only door, rules for every chat, safety labels, D1

1. **Ask D1 first** ([§2](#2-decisions-only-the-owner-can-make)). If the answer is *preview*: run and roll
   back by default — the add-in already treats a run without `apply` as a preview — hand back a one-use
   approval as `revit_apply_move` does, and re-count before keeping ([Article 12c](../../../../HERON_CONSTITUTION.md)).
   If the answer is *record the exception*: write the decision (per §1's register rule) and amend Article 9
   by the Constitution's own amendment section. **Either way, the code and the Article agree at the end.**
2. **A read-only door.** 148 fragments are PROVEN at READ or ANALYZE (derived 2026-09-23:
   `for f in brain/fragments/*/fragment.yaml; do grep -h '^heron-status:\|^risk:' "$f" | tr '\n' ' '; echo; done | grep PROVEN | grep -c -E 'READ|ANALYZE'`),
   and a chat can reach them only through `revit_change`, which needs Changes ON. The add-in already has
   `run_fragment_read` at Analyze ([`HeronOperationRegistry.cs`](../../../../platform/Heron.Core/HeronOperationRegistry.cs), verified),
   and only the command-line client sends it. Add one tool that sends it: aims at the pinned document,
   **refuses anything above ANALYZE**, prints the result and the fragment's proof status. Register it in
   `heron_tools.TOOLS`, correct the server's own text that tells the host it can already run reads, and add
   the tool to `heron-model-auditor`'s list (`tests/test_agent_tools.py` keeps that list honest). **First,
   look in DECISIONS and OPEN-QUESTIONS for any decision that keeps reads off MCP on purpose** — the
   reader found none (reported).
3. **Rules that reach the AI in every chat.** The server is built with no `instructions`, and nothing in
   `mcp/` calls the Instruction Registry ([`brain/heron_instructions.py`](../../../../brain/heron_instructions.py) —
   versioned, testable, Articles *assembled* from the Constitution, never copied) — while the
   Constitution's own Enforcement table says its Articles are *"also injected into agent instructions:
   yes"*. Wire the registry into the server. Content, kept short ([19](../../../19-context-and-cost.md)):
   the Articles a modeller's session needs; **use Revit's own Undo** — one named entry per job
   ([Golden Rule 16](../../../14-golden-rules.md)) — never a reversing change; **a passing check means the
   model meets the values given — never "compliant"**, the engineer or the authority decides; **an
   unfamiliar word is looked up, and asked about when it is not recorded** ([D-33](../../../DECISIONS.md),
   [D-34](../../../DECISIONS.md) — the store is C5's). Make the Enforcement table true.
4. **Safety labels.** MCP annotations on every tool, derived from its risk in `heron_tools.TOOLS` —
   READ/ANALYZE read-only, MODIFY and above destructive, the two model-changing tools not idempotent — with
   a test that no label disagrees with the registry. The add-in stays the real gate.

**Owns:** `mcp/server/`, `brain/heron_instructions.py`, the tool list in `heron-model-auditor` (both hosts'
copies), their tests, [04](../../../04-heron-mcp.md).
**Does not touch:** `revit/`, `brain/fragments/`, `tools/check-docs.py`.
**On the PC:** a read question answered through the new door with Changes OFF, on a named model; the rules
visible in a fresh chat; if D1 is *preview*, a preview then an apply on a named model.

### C3 — Add-in safety, and an API signature lookup

1. **Revit failure handling for fragment writes** ([01 §3 H4](01-findings.md)). Give the fragment path the
   discipline the move path already has: count and dismiss warnings, roll back on errors, **never let
   Revit's default resolution apply** (it can delete elements), report what was dismissed. Follow
   [`revit-addin-conventions`](../../../../.claude/skills/revit-addin-conventions/SKILL.md) — threading,
   one undo, error wording. Compile on all eight releases (`tools/check-compile.py`).
2. **"Has this changed in central?"** In the write pre-check, which today skips only elements owned by
   another user (`RevitWrite.cs`, reported), skip an element that is not current with central too, with the
   reason — `WorksharingUtils.GetModelUpdatesStatus` (reported present in every API release; confirm with
   `tools/check-api-surface.py`). It may contact the central server: measure the cost on the PC.
3. **An API signature lookup** — a `--members <Type>` mode in `tools/api-surface/`: parameters, return
   type, get/set and `[Obsolete]` for all eight releases, from the package copies `tools/api-changes.py`
   already caches. No Revit needed. Heron's API files record today that a changed *signature* is something
   they cannot see (reported). C7's agent uses it.

**Owns:** `revit/Heron.Revit.Addin/RevitFragment.cs`, the pre-check in `RevitWrite.cs`, `tools/api-surface/`,
their tests and README rows. **On the PC:** build and deploy every release, then prove 1 and 2 ([05](05-pc-proving.md)).

### C4 — Revit traps, written into the fragments they affect

Observed on real models by another tool, mostly on Revit 2020 — **so each is written as "observed
elsewhere, not yet proven here"** and gets a NEEDS-CHECKING row. Items marked *reported* are checked in the
file before anything changes.

| | The trap | Fragments | What to do |
|---|---|---|---|
| 1 | Connecting an air terminal to a duct can **move the terminal** — seen lifting one 625 mm — while the code says *"NOTHING IS MOVED"* (verified) | `connect-air-terminals` (DRAFT) | Correct the claim; read each terminal's position before and after and **report** any movement; a negative case |
| 2 | Resizing a connected run can leave fittings at the old size and add transitions — 22 pipes became 41 elements; unions became back-to-back transitions with open connectors | `set-mep-size`, `auto-size-mep` (PROVEN), `auto-size-pipe` (DRAFT) | Notes on all three; an open-connector count before and after in the DRAFT one |
| 3 | Rewriting a run's curve can delete takeoffs and orphan branches while the terminal still reads connected; a plain rejoin can later merge into one duct — use a union fitting | `split-mep-run` (PROVEN), `trim-extend-elements`, `set-mep-slope`, `offset-elements` | Notes; negative cases where the fragment is DRAFT |
| 4 | Placing through the API ignores the family's Default Elevation — 18 units landed at level 0 | `place-family-instances` | Note, and the skills in C9/C10 carry it as a precondition |
| 5 | "Is it in the room" is a **volume** test: a ceiling-void device is outside the room's volume. `count-by-spatial-container` and `check-room-mep-completeness` disagree about exactly that (reported) | those two, and `FILTER_ELEMENTS_IN_ROOM` | Record the contradiction; settle each with evidence, not by picking one |
| 6 | `find-clashes` counts a failed intersection test as clear; `find-dead-ends` and `find-system-islands` decide by a few category names and miss Plumbing Equipment, new in 2024 (reported) | those three | Record, then fix — a fix in PROVEN code owes a re-proof |
| 7 | Insulation and lining take their host's colour — **the owner's standing rule**. A category override drops the surface fill on ducts, pipes, air terminals, sprinklers, trays and conduit; `IsCuttable` predicts only the cut half | `color-by-parameter`, `override-graphics-in-view`, `set-category-solid-fill` | Notes, and the colour fragments follow the insulation rule. **Read open PR #300 first** — it changes the override read-back |
| 8 | A level's elevation used where the project elevation is meant (reported, in `dimension-grids-and-levels`) | that one | Verify, then fix or dismiss with the reason |
| 9 | Lower priority: openings in walls and in beams take different API calls; a room tag moved out of its room needs a leader; changing a family's category drops parameters bound only to the old one; `select-touching` runs a slow filter over the whole model | `create-opening`, the tag fragments, `check-category-mismatch`, `select-touching` | Notes |

**Owns:** the fragment folders above, nothing else. **On the PC:** every row that changed code is proved on
a named model, with a negative case ([05](05-pc-proving.md)).

### C5 — The owner's words and standards: knowledge, not language

*Wave 2 — touches the server's write path, so it starts after C2 has merged.*

1. **His site words.** [D-34](../../../DECISIONS.md) says it in its own consequences: a site word that maps
   to a Revit word *"is knowledge — it belongs in Heron's own knowledge store where it can be looked up and
   corrected"*. The Keyword Agent ([`brain/heron_keywords.py`](../../../../brain/heron_keywords.py)) is built
   for exactly that — every entry names a person and a date, an inference is never a record, an unknown
   word is a question — **and it has no store, and nothing calls it** (reported). Read
   [PROPOSALS F16](../../../PROPOSALS.md) first. Build: the store, in the owner's scope; an import that
   records reviewed entries with who and when; and a way for the host to look a word up that does **not**
   rewrite anything (D-34). **The product ships it empty.** The 52 approved entries in
   [03 §A](03-owner-data.md) are imported on the owner's PC ([05](05-pc-proving.md)).
2. **His grayout standard** ([03 §C](03-owner-data.md)) as a **company-scope** standard —
   `heron_standards` reads `company,project` by default (verified). Offered **by name, as a question**,
   never applied silently ([D-83](../../../DECISIONS.md), [D-33](../../../DECISIONS.md)); the
   `mep-grayout` skill points at it; the open choices go to the owner one at a time (**D4**). **One undo
   for the whole job:** the add-in accepts setup write steps inside one transaction group, and
   `revit_change` never sends them — [FRAGMENT-ISSUES row 141](../../../FRAGMENT-ISSUES.md) governs it.
3. **His practice values** ([03 §D](03-owner-data.md)) as a company-scope standard, offered by name: *"your
   office uses 300 mm — same here?"* — never a default.

**Owns:** `brain/heron_keywords.py` and the store code it needs, `brain/skills/mep-grayout.yaml`, the setup
chain in `revit_change`, their tests. **On the PC:** the imports, and a grayout run as one undo on a named
model.

### C6 — A score for the owner's real questions

Heron's routing has been measured only on sentences the fragments declare about themselves — which its own
history file calls a near-circular lower bound (reported) — **never on how the owner actually asks.**

Build: the **79 questions** in [03 §B](03-owner-data.md) as test data, each with the capability or skill
that *should* answer it — proposed from Heron's capability list, **never from what the search returns**,
and confirmed by the owner in one pass; a scorer that asks each question through the same function
`heron_lookup` uses and reports **first place, top three, found-but-low and not-found** separately, plus
exact matches separately and **any question that lands on a write** ([D-86](../../../DECISIONS.md)); each
run stamped with date, fragment count and backend, appended to `brain/retrieval-history.md`, printing what
moved since the last comparable run; and a test proving it catches a planted drop. **D3** decides whether a
drop only reports or fails.

**Never** add or weaken an utterance to win a row, and never rewrite a question to suit the search — that
is the forbidden move in [FRAGMENT-ISSUES row 113](../../../FRAGMENT-ISSUES.md), and D-34's.
**Owns:** the test data, the scorer, its test, one appended row in `brain/retrieval-history.md`.
**On the PC:** nothing. A cloud session has its own store, which makes it the safer place for routing work.

### C7 — A fragment-writing agent

A helper for when nothing in the library does the job. **Search first** (`heron_lookup`, `heron_resolve`,
the capability list): if a capability exists or nearly does, **widen its owner — never a second fragment for
one job** ([08 B9](../plugin-extension/08-lessons-from-the-brain.md)). Otherwise write the card (contract,
risk, utterances within [D-86](../../../DECISIONS.md)) and `impl/any/fragment.cs` at **DRAFT**; compile on
every release; run `check-routing` and `check-declared-questions`; write the proof plan with its negative
case ([`fragment-proving`](../../../../.claude/skills/fragment-proving/SKILL.md)); **never run it on a live
model** ([Article 11](../../../../HERON_CONSTITUTION.md)). Uses C3's signature lookup.

Both hosts' copies (`.claude/agents/` and `.codex/agents/`, held together by `tests/test_host_agents.py`),
a row in the [agent registry](../../../28-agent-registry.md), and a tool list with nothing that changes the
model or the chat. **Also correct `.claude/agents/skill-author.md`**, which calls itself the working
prototype of the Fragment Creation Agent while writing house-rule skills.
**On the PC:** its first real use — one missing capability from C9, written and then proved.

### C8 — Three fragments from the outside-library notes

1. **Ceiling and ray checks that see linked models** — `check-surface-fit`, `check-ceiling-coordination`
   and `check-valve-accessibility` say links are not hit (reported); Heron's own `move-to-ray-hit` and
   `check-obstructions` already handle links — follow them. Links only when asked, and say how many were
   read ([D-59](../../../DECISIONS.md)).
2. **"Changed in central?" as a column** in the element-ownership report fragments (the write pre-check
   half is C3's).
3. **Placeholder sheets** — a new fragment on `ViewSheet.CreatePlaceholder`; and first record that
   `create-sheet-list`'s card says `CREATE_SHEET` makes placeholders, which it does not (reported).

**On the PC:** a model with a linked architectural model, a workshared central, and the new fragment proved.

### C9 — Skills that carry a method: the design, then the HVAC methods

Today a Heron skill's keys are `domain`, `id`, `name`, `needs`, `preconditions`, `purpose`, `revit`, `risk`,
`source`, `utterances` and its metadata — **no order and no method** (verified), and
[FRAGMENT-ISSUES row 141](../../../FRAGMENT-ISSUES.md) records that nothing can run a skill's plan. The
earlier library's skills were methods, with the Revit lessons written into each step.

1. **Ask D2**, record the answer, then teach every reader of the skill format:
   `tools/check-skill-routing.py`, `tools/prove-skill.py`, `tools/generate-skill-catalog.py` and whatever
   reads `brain/skills/` — and make the method reach the host (today `heron_capabilities` shows a skill's
   name and first example only — reported).
2. **Terminal layout:** count from the space's airflow — the larger of a minimum and the supply divided by
   the most one terminal may carry, rounded up; as many returns as supplies; near-square rows; supply and
   return **alternating** (his "zig zag"); flow per terminal = total ÷ count; check the pattern by row and
   column. Declare what it reads and writes, which the skill does not today (reported).
3. **Space airflow:** a space for each room; **return airflow is ignored until the space's return-airflow
   mode is set first**; push a changed flow to the terminals already placed.
4. **Duct routing, FCU to terminals** — a new skill: 32 of the 120 named jobs in the owner's old log
   ([01 §5](01-findings.md)). Chain what exists (placing, rotating, drawing duct, connecting open ends,
   auto-sizing, checking for islands), and write what does not with C7's agent: a **branch builder** (riser,
   elbow, takeoff), a **duct end cap** (from the routing preference, sized and turned), and **the reducer
   200 mm after the takeoff** — his rule, [03 §A](03-owner-data.md) #51.

Every number is asked, or offered by name from his standards ([D-33](../../../DECISIONS.md)); a write
declares a question only when answering needs the write ([D-86](../../../DECISIONS.md)).
**On the PC:** each method end to end on a named model; each new capability proved.

### C10 — Tagging and sprinkler methods

1. **Tag placement that chooses well** — a new capability: try both sides at three points, with and without
   a leader, prefer the side the neighbour used, read the view scale first, point the leader downstream;
   then find overlaps, and push apart only what is left (placing first and pushing apart afterwards was
   measured elsewhere leaving about half the tags on the wrong side). His tag values come from C5's
   standard.
2. **Sprinkler layout method.** The limits come from **the company's licensed NFPA edition in company
   scope** through `heron_standards` — never from second-hand notes. The obstruction rules; the deflector
   cases — under a ceiling, under the slab and beams, and a deep void taking uprights inside and pendents
   below (his *"upright and pendant also"*); stop answering *"check my spacing"* with a coverage radius,
   which is not an NFPA idea; **never say "compliant"** — the authority decides; the hazard class is an
   input ([D-33](../../../DECISIONS.md)).

**On the PC:** named models, and his NFPA edition loaded into company scope.

### C11 — Family creation, last

The largest and the job done least often. Missing capabilities: set a family's category, add a parameter,
a reference plane, an extrusion, alignment and locking, a connector. The proof is a resize test with
several values read back. Void cuts were never solved in the earlier library. Only after C7 and C9.
