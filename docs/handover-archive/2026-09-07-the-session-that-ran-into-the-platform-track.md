# HANDOVER — the session that ran 2026-09-07 into 2026-09-08 (the platform track)

> **Archived session note.** This is a record of work that is finished. It was moved out of
> [`HANDOVER.md`](../HANDOVER.md) on 2026-09-12 so that file could go back to being the short
> live entry point it is supposed to be. Nothing here is specification: where it disagrees with
> [DECISIONS.md](../DECISIONS.md), the [Golden Rules](../14-golden-rules.md) or the
> [Constitution](../../HERON_CONSTITUTION.md), **those win**.

---


**What happened, in one line: six agents were built, and every one of them found something that was
already true and that nobody could see.**

**It ran beside the fragment-proving track and neither touched the other's files.** That track took
`PROVEN` from 16 to 50; this one did not touch a fragment's status. Staging was by explicit path all
session, and the two tracks' commits interleave cleanly in the log.

| | Start | End |
|---|---|---|
| Agents with code | 64 | **70** of 250 |
| MCP tools served | 10 | **13** |
| `tools/*.py` | 12 | **14** |
| `tests/test_*.py` | 25 | **30** |
| Agents rejected after measuring | — | **7** |

### The six, and what each one found

| Agent | Found |
|---|---|
| `AHR-GAP-001` Capability Gap | 131 compile failures on 09-06 and none on 09-07. The fix had landed and nothing recorded that it had |
| `FRG-MTX-009` Compatibility Matrix | Everything compiles on all eight releases, and **every proof Heron holds stands on Revit 2024 alone** |
| `OPS-DIA-005` Self-Diagnostics | The two above, in the one place a person would look. Four commands became one |
| `DOC-FRG-004` Fragment Catalogue | 349 fragments had no readable index. "What have we got" had been answered five times that day with throwaway Python |
| `WSP-BAK-010` Backup | **Nothing in the repository backed up anything.** The audit trail is not in git and is the only record of what Heron has done |
| `WSP-RST-011` Restore | Shipped in the same commit, because docs/21 s8 calls an untested restore path a belief. `drill` does the round trip into scratch |

`tools/agent-count.py` was built first, to answer the question that started the session, and its own
first run is the reason the next paragraph exists.

### Four things that were wrong and are now not

**Phase 0/1 was never four agents short.** A build-state summary said it was and recommended building
the Orchestrator. `HERON-ORC-MAIN/INT/PER/SUM` are **host-provided by [D-01](../DECISIONS.md)** and
`check-metadata.py` had said so all along. The register carries three states now — BUILT, HOST, LEFT —
because collapsing HOST into LEFT produces a to-do list with four items nobody will ever do.
**45 built, 4 host-provided, 0 outstanding.**

**The audit trail could not name a fragment.** 564 runs all logged as `run_fragment_read` and not one
said which. The dispatcher records it now, read from the REQUEST because a run that fails to compile
has no answer to name itself in. Durations were strings, so `"9"` sorted above `"6620"`; they are
numbers now, and both shapes are read for ever because the trail is never pruned.
**Not yet seen — the add-in carrying that change has not been deployed.**

**2,332 declared test cases had never been read by anything.** Every fragment carries
`tests/cases.yaml` with `positive`, `negative` and `second_route`; a grep for the filename returned
nothing. **Twelve did not parse, and three of those twelve belonged to fragments already at `PROVEN`.**
All twelve repaired without changing a word — the token stream was compared per file before writing.
`heron_fragment.validate()` reads them now.

**The run plan was issuing advice that had already been disproved.** `negative_case_plan` said "run it
with NOTHING selected" for every selection-fed fragment — the instruction that produced 36 refusals and
zero proofs on 09-07. NEEDS-CHECKING recorded the lesson the same day; the function issuing it was never
touched and repeated it for a week. It now says *select something containing none of what it reports*,
and shows each fragment's own declared case. **Eighteen fragments still declare "an empty element list";
`plan --all` flags every one, and none is blocked — all eighteen keep a workable case.**

### Seven agents rejected, each after measuring rather than guessing

| Rejected | Because |
|---|---|
| Duplicate Detection | 23 "duplicates" are the `transfer-*-between-documents` family being a family |
| Citation / Source | All 349 declare `source: OFFICIAL` — one value in the column it exists to distinguish |
| Naming Validation | **0** violations across 349, including capability-matches-slug |
| Keyword / synonyms | Routing is 81% first-hit, 96% top-three by words. The weak half is semantic, which synonyms do not fix |
| API Change Intelligence | Fragment API coverage is already answered by compilation. Silent behavioural change cannot be read out of assemblies |
| Fragment Performance | Needs the fragment names the dispatcher now records. **Real work, blocked on deployment** |
| Skill Research | Built it, ran it: 14 findings, **1 real**. The resolver never says "I do not know", so any sentence fragment resolves to something. Deleted |

**The pattern is the deliverable.** Rejecting on measurement took longer than building, and it is why
the six that shipped each found something.

### Read this before trusting a green result here

**`tools/check-structure.py` is FAILING on `main` as of `83fd7e8`**, and it is not from this track.
`brain/fragments/read-space-loads/impl/any/fragment.cs` writes
`catch (Autodesk.Revit.Exceptions.ApplicationException)` — the only one of 349 fragments to name the
namespace — and the layering rule forbids `Autodesk.Revit` outside `revit/`. The qualification looks
deliberate, since `ApplicationException` exists in `System` too. **It is a real tension between a
fragment needing to disambiguate and a rule written for Heron's own layers, and it is a decision for
whoever wrote it rather than something to quietly rule around.**

Everything else is green: 30 tests, `check-docs`, `check-metadata`, `agent-count`, and all eight
releases compile with 0 warnings.

### The three things waiting on Revit

1. **The trail recording which fragment ran.** Needs the add-in rebuilt and deployed. Until then
   `heron_gaps` can count failures and not attribute them.
2. **The corrected negative-case advice used on a real proof run.** The plan is right now; nobody has
   followed it yet.
3. **`heron_diagnose` and `heron_gaps` reading a trail that has fragment names in it.** Both are built
   and both are reading a trail that predates the change.

### Two habits worth keeping from this session

**A checker that has never refused anything is a claim about the checker.** Every gate built here was
fired against deliberately broken input before it was believed — `agent-count`'s five, the cases gate's
five, the backup's damaged-copy refusal. Two of them were wrong the first time and only the firing
showed it.

**A green result minutes old is not necessarily still green.** `test_validate_agent` passed
individually and failed in the full suite twenty minutes later, because the other track proved
`count-elements` in between and a test was asserting it was still unproven. With two sessions in one
tree, run the suite last.

---
