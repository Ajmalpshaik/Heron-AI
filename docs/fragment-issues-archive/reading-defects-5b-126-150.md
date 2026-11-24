# FRAGMENT-ISSUES archive — section 5b, rows 126 to 150

> **Finished rows, moved out of the live register.** These are rows from section 5b of
> [`FRAGMENT-ISSUES.md`](../FRAGMENT-ISSUES.md) — Heron's own defects found by reading the repository — whose state said they were
> finished when they were moved. Each still has its line in the register: the same number, a
> one-line title, the opening of its state, and a link to its full text here.
>
> **Nothing below was rewritten** except its links, which were re-pointed one folder deeper and
> each checked to reach the same file it reached before. Where a row disagrees with
> [DECISIONS.md](../DECISIONS.md), the [Golden Rules](../14-golden-rules.md) or the
> [Constitution](../../HERON_CONSTITUTION.md), **those win** — a row records what was true on
> the day it was written.
>
> **Nothing new is written here.** A new defect goes in the register, and a row here that turns
> out not to be finished is reopened **there**, on its own line.
>
> Rows 126 to 150 of section 5b belong in this file. A number in that band with no entry below
> was never moved — it is still in the register, in full. Written by
> [`tools/archive-fragment-issues.py`](../../tools/archive-fragment-issues.py).

---

### Row 5b-131

*Moved from the register on 2026-11-17.*

**Defect.** **A MACHINE'S NAME IN `by:` IS COUNTED AS A SIGNATURE BY THE GATE THAT COUNTS SIGNATURES.** Found 2026-09-22 beside row 5b-130. `check-signatures.py` reads `proof['by']` and counts anything non-empty. **MEASURED**: a proof signed `Claude Opus 5, at Ajmal PS's PC` is counted in *Signatures in the library* and listed under STALE like any other. **D-30 says the machine gathers evidence and a PERSON signs**, and `resign-machine-proofs.py` exists because sixteen fragments carried that exact string - in its own words, *it is not a signature, it is the thing the rule exists to forbid, written into the field meant to prevent it*. So the gate that guards the owner's signature counts the forbidden thing as the thing it forbids. **NOT A LIVE FAILURE**: measured, zero fragments in the library are machine-signed today, and `resign-machine-proofs --list` says so. But **nothing in CI runs that tool**, so nothing would notice the first one

**State.** **MOVED TO ISSUE [#914](https://github.com/Ajmalpshaik/Heron-AI/issues/914) 2026-11-16** - still open, tracked there; this row is kept whole. Was: **OPEN - measured and deliberately NOT fixed, because the question is WHERE the answer lives rather than what it is.** Two tools need to answer *is this a person's name?* and neither should hold a second copy of the other's word list - the duplicated-constant failure this repository keeps catching. Three ways, and it is a tooling decision: (1) a `MACHINE_NAMED` constant and helper in **`brain/heron_fragment.py`**, which already owns `can_promote`, `proof_problems` and `proof_is_stale`, with both tools asking it - but `resign-machine-proofs` imports no brain module today and is deliberately runnable on `fragment.yaml` text alone; (2) **`check-signatures` imports the constant from `resign-machine-proofs`** by path, which is a CI gate reaching sideways into a hand-run tool; (3) **a CI step that runs `resign-machine-proofs --list`**, leaving both files alone and putting the answer where it already is. **The measurement is the deliverable here** - `tests/test_check_signatures.py` section 5 prints it rather than asserting it, on purpose, because asserting today's behaviour would lock in the thing this row is open about

---

### Row 5b-136

*Moved from the register on 2026-11-17.*

**Defect.** **FOUR TOOLS DECLARE THEMSELVES `Heron-Layer: brain`, AND THE STANDARD NEVER SAYS WHAT THE FIELD MEANS.** Found 2026-09-22 beside row 5b-135. **MEASURED across the repository**: of everything in `tools/`, four files say `brain` - `check-routing.py`, `check-intrusion.py`, `check-skill-routing.py` and `check-risk-crossings.py` - and every other tool says `tool`. `docs/29-metadata-standard.md` lists the six permitted values and **gives no definition of any of them**, and `check-metadata.py` only asks that the value is one of the six. **THE FOLDER IS NOT THE ANSWER EITHER**, measured: every file in `mcp/` says `bridge`, three files in `revit/` say `bridge` too, and three in `tests/` say `revit`. So the field plainly means something other than the folder name - and `tools/check-products.py` is the one place that enforces a relationship between the two, refusing the product manifest if it does not say `platform` because *it lives in platform/*. Four files answer one way, forty answer another, and nothing in the repository can say which is right

**State.** **MOVED TO ISSUE [#915](https://github.com/Ajmalpshaik/Heron-AI/issues/915) 2026-11-16** - still open, tracked there; this row is kept whole. Was: **OPEN - measured and deliberately NOT fixed, because the answer is a DEFINITION and that is the owner's to write.** Two readings are defensible and the repository contains evidence for both. (1) **The layer a file BELONGS to**, which makes `brain` right for four tools that measure retrieval and exist to answer questions about the brain - and makes `bridge` right for `mcp/`. (2) **The layer a file IS**, which makes `tool` right for anything in `tools/` whatever it reasons about - and is what `check-products.py` enforces for the one file it checks. **NOTHING IS BROKEN TODAY**: the field is validated only for membership, and `check-structure.py` does its layering from the FOLDER rather than from this header, so no rule turns on the four. What is missing is a sentence in `docs/29-metadata-standard.md` saying which reading governs - and once it exists, either four files change or forty do, which is exactly why guessing it from here would be the wrong kind of tidy

---

### Row 5b-145

*Moved from the register on 2026-11-17.*

**Defect.** **THREE AGENTS ARE COUNTED AS BUILT ON THE STRENGTH OF A TEST SUITE'S HEADER, AND THE TWO TOOLS THAT ANSWER "IS IT BUILT" DISAGREE.** Found 2026-09-22 reading `tools/generate-contract-reference.py`. **The tool itself is sound and this is what it correctly REPORTS** - on every run, in its own words, and nothing records it. **THE DISAGREEMENT, MEASURED.** `tools/generate-contract-reference.py` separates them in terms: *"A SUITE IS NOT THE AGENT. tests/ files carry the same `Heron-Agent` header - which is right, it is how a suite says what it proves - ... It is just not read for what the agent does."* `tools/agent-count.py`, which **owns the count**, lists `"tests"` in its `SOURCE_ROOTS` and makes no distinction: any file claiming an agent makes it BUILT. **THE THREE**: `HERON-DEV-INT-012` claimed only by `tests/test_bridge_roundtrip.py`; `HERON-RAG-DUP-012` and `HERON-RAG-RIX-011` claimed only by `tests/test_maintenance.py`. Confirmed by calling `agent-count.built()` directly - all three come back **built**, with a suite as the only claimant. **WHAT IT MOVES.** `agent-count` reports `0 outstanding`, `docs/28-agent-registry.md` publishes **Totals: 250 agents**, and `tools/balance-of-work.py` row 3 reads **0 of 250 agents left to build**. If a suite-only claim is not a build, that zero is wrong by three. **ONE OF THE THREE IS ALREADY THE OWNER'S AND IS NOT NEW**: `DEV-INT-012` is named in [`docs/DECISIONS.md`](../DECISIONS.md)'s F27 table - *"tests/test_bridge_roundtrip.py and tests/Heron.Bridge.TestHost fit the row exactly"* - and that decision closes two of four and says **"the other two are unchanged and are the owner's"**. **THE OTHER TWO ARE NOT RECORDED AS A DEFECT ANYWHERE**, and the plan notes say what the code does not: `docs/work-notes/plans/rag/02-implementation.md` gives `RAG-RIX-011` as *"Re-index on change. The hashing exists; nothing fires it"* and `RAG-DUP-012` as duplicates at write time, both marked for Stage 6. **THIS IS ROW 5b-95's SHAPE A SECOND TIME**: that row exists because the same page *"ALREADY REPORTS THESE ON EVERY RUN and nothing records them, so they are re-discovered and never closed"*. This is the other half of the same page's output, and it had the same fate. **RAISED, NOT FIXED, AND IT IS A DEFINITION**: does a claim by a suite alone count as built? Changing `agent-count`'s `SOURCE_ROOTS` would move a figure in `docs/28`, in `agent-count`'s own reconciliation and in the balance page, and guessing which way is the wrong kind of tidy. **The owner writes the sentence; the two tools then follow it.**

**State.** **MOVED TO ISSUE [#913](https://github.com/Ajmalpshaik/Heron-AI/issues/913) 2026-11-16** - still open, tracked there; this row is kept whole. Was: **OPEN - the owner's.** Neither tool was changed. `tools/generate-contract-reference.py` is marked **clean** in the review ledger: it is right, and it is the only thing in the repository that says this out loud.

---
