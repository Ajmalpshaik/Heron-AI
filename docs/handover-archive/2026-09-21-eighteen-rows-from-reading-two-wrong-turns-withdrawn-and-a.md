# Session note — EIGHTEEN ROWS FROM READING, TWO WRONG TURNS WITHDRAWN, AND A GATE THAT HAD NEVER RUN

> **Archived session note** from 2026-09-21. It was moved out of [`HANDOVER.md`](../HANDOVER.md) on
> 2026-09-23 by [`tools/archive-handover.py`](../../tools/archive-handover.py), so that file could go back to
> being the short live entry point it is supposed to be. **Its words are unchanged; only its links were
> re-pointed.** Nothing here is specification: where it disagrees with [DECISIONS.md](../DECISIONS.md),
> the [Golden Rules](../14-golden-rules.md) or the [Constitution](../../HERON_CONSTITUTION.md), **those
> win**. A note records what was true on its own day.

---

### 2026-09-21 — EIGHTEEN ROWS FROM READING, TWO WRONG TURNS WITHDRAWN, AND A GATE THAT HAD NEVER RUN

**PRs #228, #229, #230, #231 merged; #232 open.** Section 5b rows **60 to 77**. A reading session, not
a proving one — nothing here needed Revit and nothing here proves anything about a model.

**THE THREAD THROUGH ALMOST ALL OF IT**: not that somebody wrote something false, but **a sentence that
was checked the day it was written and never again**. The library moved past it, or the mechanism it
described was changed by the same session that left it standing.

| row | what it was |
|---|---|
| **60** | `change-evidence.py` merged NOT RUN into FAILED, three lines below a gate that does not — and [row 49](../FRAGMENT-ISSUES.md) left **seven** descriptions of CI behind, two hours after it landed |
| **61** | *"106 `READ` fragments carry a proof against 63 `MODIFY`"* — correct when written, backwards at 145 against 166 |
| **62** | *"a real Revit has been used on 2020 and 2024"* — it is **three**, and `docs/16` still asked somebody to confirm 2027 |
| **63** | every refusal in `review-ledger --mark` exited **0**, so two silent misses in one sitting |
| **64** | `AGENTS.md` and `PROJECT-MAP` called `.claude/skills/` *"the **only** skills tree"* while `brain/skills/` holds the ten skills that **are** the product |
| **65** | the word D-84 withdrew is still in **Constitution Article 11**, **Golden Rule 18** and the README's Decided table — **recorded, not fixed** |
| **66** | the size of the library typed as a present-tense fact in **eleven live places**, wrong by up to 52 |
| **67** | `check-docs.py`'s sentence splitter did not break on `.**`, so a history word excused the *next* claim — in three gates this sweep had just added |
| **68** | `Directory.Build.props` said *"Step 1 proves ONE version"*; eight compile on every push |
| **69** | a **comment** edit inside a `PROVEN` fragment broke D-30's fingerprint and CI rejected it |
| **70** | the CI job named *"The gates that must pass"* ran **nine** commands while four documents said four |
| **71** | `gates.yml` said two checkers *"say so rather than failing"*; on Linux they died with a traceback |
| **72** | `process_is_running()` promised *alive, dead or cannot tell* and had a fourth outcome: **never** |
| **73** | `change-evidence` could not run two gates `check-change` demands, so every C# change came back REVISE |
| **74** | `check-compile` reported a toolchain error as *"fix your code"* — and **the diagnosis I wrote first was wrong and is recorded as withdrawn** |
| **75** | two lifecycle ladders, one confirmed and one **proposed**, and the code is split — **not resolved here** |
| **76** | the product manifest says the owner has not ruled on Q-PE-2. He ruled the same day |
| **77** | `check-products.py` guards the manifest the installer reads and **had never run anywhere** |

#### TWO THINGS I GOT WRONG AND WITHDREW, BOTH RECORDED RATHER THAN QUIETLY DELETED

**Row 75 is the one worth reading.** I found `SHADOW` missing from four of the lifecycle ladder's
declarations, changed all four to match [24](../24-trust-model.md), and opened a PR. **docs/24 is a
PROPOSAL the owner has twice declined to sign off** — its own header says *"proposes a resolution and
needs confirmation, see Q-34"*, `docs/README.md` says *"two items need confirmation before building"*,
and [D-14](../DECISIONS.md) stays Proposed because he answered ***"yes, but show me on screen first"***,
twice, eleven days apart. `heron_fragment.STATUSES` without `SHADOW` is [00 §18](../00-master-specification.md),
the CONFIRMED ladder. **It was right.** Reverted in full; `brain/` is byte-identical to before.
What ships instead is a suite that **reads both ladders and reports which side each declaration is
on**, failing only on one matching neither.

**Row 74's first draft** blamed a missing .NET Framework targeting pack for `check-compile` failing
Revit 2020 here. Measured instead: a bare `net472` project built first try, the same command passed
all eight releases minutes later, and CI had compiled 2020 green throughout. **A cold NuGet cache, and
one unreproducible failure is not a finding.**

#### WHAT NEEDS WINDOWS OR REVIT — NONE OF IT WAS TOUCHED, AND NONE OF IT COULD BE

This session ran on Linux with no Revit. Everything below is **unchanged and still owed**, and it is
the larger half of the remaining work:

| | derive it |
|---|---|
| Fragments never in front of a model | `python tools/balance-of-work.py` |
| — of those, arrangeable right now, needing only a session | `python tools/generate-jobs.py` |
| — of those, structurally blocked, each with a printed reason | `python tools/generate-jobs.py` |
| Skills never proved | `grep -h '^heron-status:' brain/skills/*.yaml \| sort \| uniq -c` |
| Proving-register rows open | `python tools/owner-queue.py` |
| Signatures gone stale — proved, then the code moved under them | `python tools/check-signatures.py` |

**Two register rows name Windows-only work explicitly and neither moved**: `E16` and `D3` — the tape
measure on the three ducts that were moved 200 mm on 2026-09-07. The two STALE signatures are
`tag-elements-in-view` and `force-tag-leader-lshape`.

**The C# side was compiled, never run.** `check-compile` builds all eight releases here and
`check-api-surface` reads 362 Revit members against all eight, but **a compile is not a proof** and
neither says the add-in behaves. Loading it is `A12`/`A13` and it needs the PC.

#### WHAT IS WAITING ON THE OWNER AND CANNOT BE CLOSED HERE

1. **[F23](../PROPOSALS.md)** — the word *sandbox* survives in **Constitution Article 11**, **Golden Rule
   18** and `README.md`'s Decided table, and [D-84](../DECISIONS.md)'s own consequence line says it is
   nowhere. The Constitution is **injected into agent instructions**, so a running agent is told a
   containment exists that was never built. Its Amendment section reserves Articles to the owner, so
   the replacement wording for all three is **drafted and not applied**. One yes or no.
2. **Q-34 / `R1b`** — which lifecycle ladder the code follows. Four declarations follow the confirmed
   one, two follow the proposal. **Only a screen closes it**, as recorded twice.

#### WHAT THE NEXT SESSION SHOULD PICK UP

**The reading sweep, and it is early.** `python tools/review-ledger.py` for the balance and
`--next 20` for the queue. Marks carry the file's content hash, so a mark withdraws itself when the
file changes — **run `--stale` first and clear anything it names before reading anything new.**

**The method that earned its keep**, and it is not sequential reading: **measure a class across the
repository** rather than read file by file. Rows 66, 70, 72, 73, 75 and 77 all came from asking one
question of every file at once. Two leads were killed the same way — a units sweep (all 82
length-shaped inputs convert, D-71 is fully applied) and an agent-refusal sweep (already covered by
`tests/test_contract_reference.py`, better than the scan that found it).

**And the rule this session paid for twice**: after changing a mechanism, **grep for the other
descriptions of it**. Row 60 records seven left behind in two hours; row 77 records a sixth place that
five files and one deliberate sweep still missed, caught only because `tests/test_tag.py` went red.
