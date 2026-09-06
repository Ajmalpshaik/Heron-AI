# Prompt — build the Fragment Validation Agent

<!-- Heron-Agent:  none -->
<!-- Heron-Step:   17 -->
<!-- Heron-Status: DRAFT -->
<!-- Heron-Since:  0.1.0 -->
<!-- Heron-Layer:  doc -->
<!-- See docs/29-metadata-standard.md -->

**Written 2026-09-07. Paste the block below into a fresh session.** It is written to be started from
cold — it names the files, the numbers, the one rule that must not be broken, and how to know it worked.

Background for a human reading this first: [HANDOVER.md PART 7](../HANDOVER.md) is the survey this came
from. Twelve agents are named in [`08-agent-catalog.md`](08-agent-catalog.md), none are built, five are
buildable now, and this is the one worth doing first because it attacks the measured bottleneck —
**333 DRAFT fragments against 16 PROVEN**, proved by hand, one at a time.

---

## The prompt

```text
Read HANDOVER.md in Heron-AI, then build the Fragment Validation Agent described in
docs/PROMPT-fragment-validation-agent.md.

Work in your own worktree per HANDOVER.md section 9b - another session edits the
same tree. Do not touch brain/fragments/ except to WRITE A DRAFTED PROOF that a
person has confirmed.

THE ONE RULE THAT MUST NOT BE BROKEN: this agent never sets
heron-status: PROVEN. It gathers evidence and drafts a proof block; a person
confirms it. D-30 exists because an unproven claim quietly ages into a believed
one, and an agent that can stamp 333 fragments is the fastest machine ever built
for doing that. Clash detection finds the clashes and lists them; the engineer
decides which are real and the engineer signs the drawing. The machine never
signs.

Start by reading, in this order:
  brain/fragments/list-levels/fragment.yaml   - a COMPLETE proof block, the shape to produce
  brain/fragments/read-selection/fragment.yaml - a proof that names what it could NOT establish
  docs/DECISIONS.md D-30                       - what a proof must contain and why
  mcp/client/heron_bridge_client.py cmd_prove  - how a fragment is run today
  revit/Heron.Revit.Addin/RevitFragment.cs     - the executor, and what it can bind

Then propose the design before writing it, and say what you will NOT attempt.
```

---

## What the agent is for

A proof under [D-30](DECISIONS.md) needs a positive case, a **negative case**, a **second route**, the
model it ran against, and a fingerprint. Producing one by hand takes a person at a PC with Revit open.
**Most of that work is mechanical and some of it is not**, and the whole value of this agent is drawing
that line honestly.

| Step | Can a machine do it? |
|---|---|
| Run the fragment, record what came back | **yes** |
| Record the model, element count, view, date | **yes** |
| Compute the fingerprint | **yes** |
| Arrange a state where the answer must differ, and run again | **yes, for many** |
| Find a second fragment reaching the same answer another way | **yes** — the dependency graph already knows which capabilities overlap |
| Decide whether the answer is **RIGHT for this building** | **no. This is the person's** |
| Set `heron-status: PROVEN` | **never** |

## What already exists to build on

- **The executor** — `revit/Heron.Revit.Addin/RevitFragment.cs`. Runs a fragment's C# in Revit's own
  process and binds its declared `needs` from the selection or from the previous fragment in the batch.
  Built 2026-09-07, PART 6, and six of Group J's checks passed against a real model.
- **`prove`** — `mcp/client/heron_bridge_client.py`. Runs several fragments in one lease and prints what
  each left behind. Batches share a chain; the first sends `chain: reset`.
- **The dependency graph** — `brain/heron_graph.py`, 349 capabilities and **0 orphans**. This is how a
  second route is found rather than guessed.
- **Staleness** — `tests/test_golden.py` already knows a proof taken against a build that no longer
  exists. A drafted proof must carry the fingerprint that makes this work.
- **The audit log** — `%APPDATA%\Heron\audit\*.jsonl`, 331 entries, **196 ok / 135 failed**. Every
  fragment run today is in there with its outcome. **Read it before running anything**: some fragments
  have already failed for reasons that have nothing to do with the fragment.

## What it must refuse to do

- **Never write `PROVEN`.** Draft into a field a person promotes. If that means a new
  `proof-draft:` key that a human moves to `proof:`, so be it — the shape is the agent's to propose.
- **Never invent a negative case it did not run.** A proof saying *"with nothing selected it returned 0"*
  when nothing checked is worse than no proof.
- **Never call two fragments a second route because their names look alike.** `LIST_LEVELS` and
  `REPORT_LEVEL_ELEVATIONS` genuinely reach the same fact by different code. `FIND_DEAD_ENDS` and
  `CHECK_FLOW_DIRECTION` do not, and they rank within 0.003 of each other.
- **Never mark a fragment it could not run.** `needs_request_values` — a category, a name, a distance —
  affects **278 fragments** and there is still no route for those. Say so, per fragment, and move on.
- **Never touch the write path.** `run_fragment_read` opens no transaction. A fragment at `risk: MODIFY`
  is out of scope entirely until Phase 1 is proven.

## How to know it worked

**Not by a count of drafts produced.** The measure is:

1. A person reviewing a drafted proof **accepts it unchanged**, more often than not.
2. At least one draft says **"I could not establish this"** and names why — like `READ_SELECTION`'s own
   proof does about an off-screen document. An agent that never reports an unknown is not looking.
3. The fingerprint on a drafted proof makes `tests/test_golden.py` correctly call it STALE after the
   fragment's C# changes.

**If it produces 300 drafts and a person rejects most of them, it has made the backlog worse**, because
now somebody must read 300 wrong things instead of proving 300 right ones. Build it to do ten well.

## Open questions for the session that builds it

- Where does a draft live — a `proof-draft:` key in `fragment.yaml`, or a separate file? A key keeps one
  fact in one home ([docs/29](29-metadata-standard.md)); a separate file keeps the library clean of
  unreviewed content. **This is a real decision and belongs to the owner.**
- Can the negative case be arranged automatically for a fragment needing a selection? Emptying the
  selection is a UI action, and `run_fragment_read` deliberately cannot act on the UI.
- Should it run against **two** models to catch answers that are true of one building only? `LIST_LEVELS`
  was proved that way and it is what caught the difference between 11 levels and 2.
