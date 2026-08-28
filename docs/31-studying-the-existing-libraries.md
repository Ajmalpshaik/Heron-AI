<!--
Heron-Agent:  HERON-IMP-FEX-004
Heron-Step:   14
Heron-Status: DRAFT
Heron-Since:  0.1.0
Heron-Layer:  brain
See docs/29-metadata-standard.md
-->

# 31 — Studying the existing libraries

**The owner has a working fragment library of several hundred entries. None of it is imported. All of it
is worth reading.** This document is how one becomes the other.

[D-25](DECISIONS.md) settled the principle — *studied and re-authored, never imported* — and deleted a
planned import pipeline rather than choosing between libraries. What it did not give was a **method**,
and without one "study it" becomes "read it and copy it more slowly."

> **The owner's instruction, 2026-08-28**, and it is the whole of this document:
> *"You can check and study everything, but don't copy-paste blindly. Check, study, and take what you
> need... 1. Check and edit. 2. If you want to add, add. 3. If you want to split, split."*

---

## 1. What is actually there

Read once, on 2026-08-28, to size the job rather than to take anything:

| | |
|---|---|
| **398** working C# fragments | organised as filters, actions, creators, recipes, context and commands |
| **221** carrying a recorded verification | against a real model, most dated 2026-08-06/07 |
| The largest areas | sheets and views (54), reporting (43), structural changes (33), QA checks (30), colour and graphics (25), parameters and naming (21) |
| **12** skills | the real jobs: HVAC terminal layout, space airflow, duct routing, fire sprinkler layout, MEP grayout, connectivity verify, MEP trace, family creation, visual reporting |

**The shape confirms [D-29](DECISIONS.md) rather than merely agreeing with it.** That library
independently arrived at filter + action + recipe — the same three kinds, for the same reason: most
requests split into *which elements* and *what to do to them*, and the ones that genuinely cannot be
split are named separately so they cannot pretend otherwise.

**It also has two kinds Heron does not**, and that is a finding rather than a gap to copy:
**creators** (36 — elements that do not exist yet and must be made) and **context** (12 — what is open,
which view, which document).

**Neither gets a new `kind` in Heron, on today's reading.** A creator's contract is
`provides: elements` — the same as a filter's. What differs is that it *writes*, and that is already
carried by `risk: MODIFY` rather than needing a fourth kind; `kind` answers *how does this compose*,
`risk` answers *what may it do*, and collapsing the two would make both worse. Context reads the
session rather than elements, which is a filter whose `provides` is a document or a view.

This is recorded as a **judgement, not a decision**, and deliberately: nothing here has written a
creator yet. Revisit it when the first one is re-authored and the contract is in front of you — that is
the same *look first, decide second* that settled four questions on 2026-08-28 by reading a system
already doing the job. **If a real creator does not fit, this paragraph is wrong and the fix is a
decision entry, not a quiet edit.**

---

## 2. What travels, and what cannot

This is the part that costs the most and is easiest to get wrong.

| | Travels | Why |
|---|---|---|
| **The mechanism** | ✅ | *There is no single "get an element's level" API; a wall stores it one way and an MEP curve another* is a fact about Revit. Facts are not owned |
| **The scar** | ✅ | *Omitting one parameter made a level filter match zero elements and report success.* This is the most valuable thing in any library and the least visible |
| **The shape of a good proof** | ✅ | Already taken: [D-30](DECISIONS.md)'s negative case came from reading proofs there that record what came back **empty** |
| **The code** | ❌ | Never. Written fresh here, in Heron's shape, against Heron's contract |
| **The words** | ❌ | Never. Heron's reasoning in Heron's sentences, or the note becomes unreadable the day the other library stops being used |
| **The names** | ❌ | No file names, folder names, tool names or branding come across |
| **The proof** | ❌ | **A fragment verified there arrives here `DRAFT`.** It is different code; a proof of the other is not a proof of this. [D-25](DECISIONS.md) says so explicitly |
| **The authority** | ❌ | *"Go and read that other repository"* is worthless the day it is retired. State the reasoning here, in full, so it stands alone |

> **The consequence, stated plainly because it is expensive:** 221 verified fragments there become 221
> **unproven** fragments here, each owing its own proof with a negative case against a real model. Study
> is cheap. Re-authoring is moderate. **Proving is the expensive part and it is gated on the owner's
> machine.** Any plan that ignores that arithmetic is planning to arrive with several hundred DRAFT
> fragments and no way to trust one of them.

---

## 3. The method — the owner's three rules, made operational

For each candidate, in order. **Stop at the first rule that applies; do not run all three.**

### Rule 0 — Does it earn a place at all?

Asked first, because the cheapest fragment is the one not written.

- **Does Heron already cover it?** `python brain/heron_fragment.py` lists what exists. Two fragments
  answering one sentence is the duplication [D-29](DECISIONS.md) exists to prevent.
- **Is the job one the owner actually does?** The 12 skills are the evidence for that, not the fragment
  count. The library's own job log is blunt about this: *the standing guess is that about 40 of the 398
  do 90% of the work, and nobody knows which.* **Heron should not inherit that uncertainty** — it
  records usage from its first day, so this question becomes answerable here even though it is not
  answerable there.
- If neither, **skip it and record nothing.** A library of everything is a library nobody searches.

### Rule 1 — Check and edit

The default path. Read the fragment for **what it knows**, then write Heron's own from that
understanding:

1. **Read the whole file, comments first.** The comments carry the scars; the code carries the result.
   A comment saying *"this one matters for MEP — the other four are not present at all"* is worth more
   than the four lines beneath it.
2. **State the contract before writing any code** — `needs` and `provides`, named and typed. If that
   cannot be stated cleanly, the fragment is doing two jobs and Rule 3 applies.
3. **Write it fresh against that contract.** Not a transcription with the names changed.
4. **Carry the knowledge into Heron's own words**, including *why*. A fix whose reason is not recorded
   gets removed by the next person who thinks it looks redundant.
5. **Set `heron-status: DRAFT` and write the proof's cases without the proof.** State what the positive
   and negative cases must be, so whoever reaches a Revit knows exactly what to run.

### Rule 2 — If you want to add, add

Heron's version may be **better than what was read**, and where it is, that is the point of re-authoring
rather than importing:

- **Add what the contract needs.** Heron's fragments declare their inputs and outputs; many read
  fragments have inputs as edit-me-first literals at the top of a file.
- **Add the version range explicitly.** `revit: [...]` as a list — never a range that claims releases
  nobody has tried ([D-05](DECISIONS.md)).
- **Add a negative case to the test file even when the original had none.** [D-30](DECISIONS.md) is not
  optional here and a fragment that cannot state one is not yet understood.
- **Add nothing speculative.** An input nobody asked for is a parameter to maintain and test forever.

### Rule 3 — If you want to split, split

Where one read fragment is really two jobs joined:

- **Split when the extracted part has at least two real consumers**, actual or clearly imminent
  ([09 §6](09-skills-and-fragments.md)). Reuse justifies a split; tidiness does not.
- **The reliable signal is the contract.** A fragment whose `provides` list has two unrelated entries is
  usually two fragments — one filter and one action that were written together because they were needed
  together once.
- **The opposite is equally real.** Over-decomposition — ten three-line fragments behind nine layers of
  indirection — is worse than one clear fragment, and it passes every check a tool can run.

---

## 4. What this is NOT

- **Not a migration.** There is no batch, no importer and no mapping file. Every fragment is a separate
  decision, and most of the 398 will correctly never be made.
- **Not a race to a number.** *"Heron has 398 fragments"* would be a claim about a folder. Ten fragments
  that are proven, composable and used beat three hundred that are none of those.
- **Not one-directional.** A mechanism understood well enough to re-author is usually understood well
  enough to improve, and the improvement stays here — the other library is **read-only**, permanently
  ([HANDOVER §7](../HANDOVER.md)).

---

## 5. Where it happens in the build

[Step 14](27-build-order.md) is the first ten, and it is the last step of Phase 2 because the machinery
has to exist before a fragment can be stored, found, composed or trusted. **After Phase 2 this becomes a
standing activity rather than a step** — the library grows when a real job needs something, which is the
only signal that has ever been worth building from.
