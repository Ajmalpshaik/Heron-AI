# HANDOVER — the session that ran 2026-09-07 (the verification track)

> **Archived session note.** This is a record of work that is finished. It was moved out of
> [`HANDOVER.md`](../HANDOVER.md) on 2026-09-12 so that file could go back to being the short
> live entry point it is supposed to be. Nothing here is specification: where it disagrees with
> [DECISIONS.md](../DECISIONS.md), the [Golden Rules](../14-golden-rules.md) or the
> [Constitution](../../HERON_CONSTITUTION.md), **those win**.

---


**What happened, in one line: nothing was taken on trust. Every proof already recorded was re-run and
all thirteen held, three more fragments earned one, and four things that cost real time are now written
down so nobody pays for them twice.**

**It ran beside PART 6, and neither track knew about the other until the merge.** PART 6 built the
executor's inputs; this track checked what was already claimed. They agree where it counts, and the
merge commit says how.

| | Start | End |
|---|---|---|
| Fragments | 348 | **349** |
| `PROVEN` | 13 | **16** |
| Recorded proofs re-run | — | **13 of 13 hold, 13 of 13 fingerprints fresh** |
| DRAFT READ fragments through the real executor | — | **135, zero unexplained failures** |

### The three that were proven, and why they could not have been before

`READ_SELECTION`, then `REPORT_PHASES` and `REPORT_DESIGN_OPTIONS` — **the last two only because the
owner made the conditions himself while the session ran.** Neither model in this repository's history
had a third phase or a design option in it, so a fragment that ignored the model and returned the
default pair would have passed every check ever run against it. **That is the whole reason D-30 asks
for a negative case**, and it is the first time the rule has visibly earned its keep on the same day it
was applied.

### One fragment was built, and two that were asked for cannot exist

`CREATE_GLOBAL_PARAMETER` — **the gap was named by this library itself.** `SET_GLOBAL_PARAMETER`'s
routing table had said *"make a global parameter → NOT HERE"* and pointed at nothing since 2026-09-06.

Creating a **phase** and creating a **design option** were asked for in the same breath and **neither is
possible on any release** — no create method, no constructor, `Document.Phases` read-only, and
`DesignOptionSet` is not a type at all. Both refusals were already written in the two report fragments'
own routing tables; reflection against the shipped assembly confirmed them rather than discovering them.
**A fragment that names its own boundary is this repository's best gap detector, and it works in both
directions: it found the one worth building and refused the two that cannot be.**

### Four things that cost time, now written down

| | |
|---|---|
| **The retrieval store never drops a deleted fragment** | The whole directory was moved off disk and the store still answered from it. Indexing is additive. **Every before-and-after taken without deleting `global.db` first is worthless** — the first set taken this session was |
| **`test_graph.py` rewrites real fragment files** | Two runs overlapped and it failed in a way indistinguishable from a broken graph deriver. Never run two sweeps at once, and never run `check-gaps.py` beside anything — it runs the suite itself |
| **The executor's read-only guarantee is a promise, not a guarantee** | `run_fragment_read` is declared `Analyze`, so neither the emergency stop nor `write.enabled` covers it, and the C# arrives in the request unread. A fragment breaking Rule 16 would write with writing switched off. **Re-verified after the PART 6 merge and it still stands word for word** |
| **A missing C# identifier does not stay a `CS0103`** | It cascades into unrelated error codes on lines that are correct. A triage rule written on that assumption reported 13 defects that are not defects |

### What the next session should do, in order

1. **The four half-proved fragments**, listed with their predictions at the top of this file. One needs
   a global parameter created by hand — **`CREATE_GLOBAL_PARAMETER` cannot do it**, because no write
   path runs a fragment yet.
2. **Group J in `NEEDS-CHECKING.md`** — the eight rows that would prove PART 6's input binding. None of
   it has met a model.
3. **The caller's half of the inputs.** 278 fragments want a category, a name or a distance and there
   is no route for one. Now the largest single unlock.

**A warning worth repeating because it was nearly paid twice:** `LIST_WORKSETS` came back looking like a
regression, and was not — its proof records the positive case on a *different model* from the negative.
**Read the whole proof before believing a re-run disagrees with it.**

---
