# Prompt — build the Fragment Validation Agent

<!-- Heron-Agent:  none -->
<!-- Heron-Step:   17 -->
<!-- Heron-Status: DRAFT -->
<!-- Heron-Since:  0.1.0 -->
<!-- Heron-Layer:  doc -->
<!-- See docs/29-metadata-standard.md -->

> ## ✅ BUILT — 2026-09-06. This is now the record of the brief, not a job waiting.
>
> [`brain/heron_validate.py`](../brain/heron_validate.py) is the agent, [`tests/test_validate_agent.py`](../tests/test_validate_agent.py)
> checks it without Revit, and `heron_bridge_client.py validate` is the half that needs a model open.
> [`brain/proof-drafts/README.md`](../brain/proof-drafts/) explains what a draft is to whoever finds one.
>
> **Nothing has been run against Revit.** Every judgement below is testable off-model and was tested;
> the running half could not be, on a machine with no Windows and no Revit on it. The three open
> questions at the foot of this file are answered, each by a measurement rather than a preference.
>
> **Two numbers in this brief were checked and one was wrong.** The reachable working set is **42
> fragments**, not 333 — see [the plan](#what-it-actually-reaches). And *"every fragment run today is
> in the audit log with its outcome"* is **not true**: the add-in records the operation, not which
> fragment ran, so no failure in that log can be attributed to a fragment.

**Written 2026-09-07. Paste the block below into a fresh session.** It is written to be started from
cold — it names the files, the numbers, the one rule that must not be broken, and how to know it worked.

Background for a human reading this first: [HANDOVER.md PART 7](HANDOVER.md) is the survey this came
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

## What it actually reaches

Measured off disk on 2026-09-06, because the roadmap's number and the reachable number are nothing like
each other. `python brain/heron_validate.py plan` recomputes all of this live.

| | |
|---|---|
| Fragments | **349** — 333 `DRAFT`, 16 `PROVEN` |
| Need a value only the request carries | **279** — no route today, and the plan says so per fragment |
| Can change the model | **180** — out of scope while `write.enabled` is false |
| Run on the document alone | **20**, and 16 of those are the 16 already proven |
| **Worth running now** | **42** — 4 standalone, 36 fed by a selection, 2 needing a batch |

**Forty-two is the honest size of this job.** It is not 333, and it is not 4 — which is what it would be
if the selection and the chain were ignored. It is enough to be worth doing and small enough to do well,
which is what the *"build it to do ten well"* line above was asking for.

## Open questions — all three answered, each by a measurement

**Where does a draft live?** *A separate file*, `brain/proof-drafts/<slug>.yaml`. Not for tidiness: a
`proof-draft:` key inside `fragment.yaml` would need the agent to hold a write path into
`brain/fragments/`, and once that exists *"it never sets heron-status"* is only true while the code
stays correct. With drafts outside, `write_draft` refuses the library outright and the rule holds
because there is nothing to break. The docs/29 cost — one fact, two homes — is real and is paid off by
`accept` deleting the draft as it records the proof.

**Can the negative case be arranged automatically for a selection-fed fragment?** *Yes in principle,
and almost never in practice today.* `select_by_category` ends in `uiDoc.Selection.SetElementIds(found)`
— it **sets** the selection, so asking for a category the model does not contain empties it, with no
person involved. The catch is the category table in `RevitOperations.cs`: it holds four spellings of one
word, `duct`. So the automatic route exists and reaches one category. Everywhere else a person clears
the selection, which is one keystroke at the machine they are already sitting at. Widening it is a table
row, not a design change.

**Should it run against two models?** *Yes, and for a standalone fragment it is the only negative case
there is.* A fragment reading the whole document has nothing to withhold from it — the empty answer has
to come from a second model that genuinely lacks the thing. That is exactly how `LIST_LEVELS` was proved
(11 levels against the 2 template levels), and `validate --negative-in "Project1"` is that shape made
into a command.

## One thing found while building it, and it is not this agent's

**All 16 proven fragments reported `STALE`, and none of them was.** `python brain/heron_fragment.py`
failed with sixteen problems in a Linux container while passing on the PC. The fingerprint hashed the
raw bytes and the path as the operating system spelled it, so it was a fact about Windows as much as
about the code — every recorded value reproduces exactly by hashing the same bytes with backslash paths
and CRLF line endings, and git shows every implementation predating its own proof date.

`fingerprint()` now normalises both, and the sixteen were re-recorded once with
`heron_validate.py restamp --apply`, which **refuses** any fragment whose code moved ON OR AFTER its
proof was taken. Only the fingerprint line changed in each file; no proof, author, date or status was
touched.

**It matters beyond the tidying.** A staleness gate that cries wolf sixteen times out of sixteen stops
being read, and this agent's whole third success criterion is that the fingerprint it drafts makes
`test_golden.py` call a proof stale when — and only when — the C# changes.
