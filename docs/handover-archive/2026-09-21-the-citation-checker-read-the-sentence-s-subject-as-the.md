# Session note — THE CITATION CHECKER READ THE SENTENCE'S SUBJECT AS THE DOCUMENT

> **Archived session note** from 2026-09-21. It was moved out of [`HANDOVER.md`](../HANDOVER.md) on
> 2026-09-23 by [`tools/archive-handover.py`](../../tools/archive-handover.py), so that file could go back to
> being the short live entry point it is supposed to be. **Its words are unchanged; only its links were
> re-pointed.** Nothing here is specification: where it disagrees with [DECISIONS.md](../DECISIONS.md),
> the [Golden Rules](../14-golden-rules.md) or the [Constitution](../../HERON_CONSTITUTION.md), **those
> win**. A note records what was true on its own day.

---

### 2026-09-21 — THE CITATION CHECKER READ THE SENTENCE'S SUBJECT AS THE DOCUMENT

**[Row 5b-102](../FRAGMENT-ISSUES.md). FIXED.** And **[row 5b-103](../FRAGMENT-ISSUES.md), OPEN**, found
while writing the first one down.

**THE TARGET WAS CHOSEN BY MEASUREMENT AGAIN, AND IT PAID AGAIN.** Re-running the sweep the last
entry describes — every module reachable from `mcp/`, distinct suites that REACH it against its
public surface, classes excluded — `brain/heron_research.py` came out **thinner than the
`heron_ground` that produced [row 5b-101](../FRAGMENT-ISSUES.md)**: 783 lines, 5 public functions,
**7 suites**. It had never been opened.

`citation()` decides what an external answer's citation is worth, and a modeller reaches it through
the `heron_research_check` MCP tool. When no issuing body is named — **which is every company
standard and every project specification, the two documents `_NAMED` was added for** — it took the
**first** proper name in the sentence and read the edition and the locator off that:

| given | document it took | verdict |
|---|---|---|
| *"Acme Engineering BIM Standard 2026, clause 3.1 requires 30mm."* | `Acme Engineering BIM Standard` | well-formed |
| *"Fire Dampers shall be rated per Acme Engineering BIM Standard 2026, clause 3.1."* | **`Fire Dampers`** | **VAGUE**, missing the edition and the clause |

**The second is the same citation with a subject in front of it**, and the report names as absent the
two parts sitting in the sentence the reader is looking at — so the only action it offers is already
done. **A Revit category name is two capitalised words**: Fire Dampers, Air Terminals, Mechanical
Equipment, Duct Fittings. On a BIM platform the subject looks exactly like a document name.

**WHY NOTHING CAUGHT IT: every case ever put through this function puts the document FIRST.** All
four in `tests/test_research.py` §3 and §8, and both in `tests/test_review_findings.py` §64 — the
round-ten review that ADDED `_NAMED`. **The input set was the gap, not the checker.** That is a
different shape from the last three rows, which were things held by a string match: this one was
held by six real behaviour checks that all shared one blind spot.

**The fix picks between candidates and never invents a part.** The document is the name the
citation's other parts ATTACH to, by the two rules the parts were already read with — the edition
sits on the reference, the locator follows it within `_LOCATOR_GAP` words. **When no candidate
carries either, the first is still taken**, so a sentence that was VAGUE before is VAGUE after, and
§10 checks that.

**Shown to FAIL, and the split is clean** — they fail rather than raise, which is the half
[heron-ship §2a](../../.claude/skills/heron-ship/SKILL.md) is about:

| put back | red |
|---|---|
| the trailing-stop half | **1** |
| the first-candidate rule | **3** |
| both (the module as found) | **4** |

**THE SECOND HALF WAS ONE CHARACTER.** The vague-source list is compared against the name stripped of
`" ,;:-"` and **not `"."`**, so a phrase ENDING a sentence kept its full stop and escaped the list:
*"per Industry Practice."* was accepted as a named document. `_NOT_A_DOCUMENT` holds *industry
practice* for exactly that sentence.

#### And marking the ledger signed the read with the owner's name

**[Row 5b-103](../FRAGMENT-ISSUES.md), OPEN — it needs one sentence from the owner, not a Revit.**
`python tools/review-ledger.py --mark brain/heron_research.py ...` came back **`by ajmal-pc`**, and no
person had read that file. `who()` takes `HERON_CLIENT_ID` first, and
[docs/38](../38-the-cloud-environment.md) sets that to `ajmal-pc` **in the cloud environment** — correctly,
for a reason entirely about the bridge lease. **One variable, two jobs**: a lease id this register
says is deliberately SHARED across a person's chats, and an author id that is worthless once shared.
24 rows in `docs/REVIEW-LEDGER.tsv` carry it, and **one carries `claude-linux`** — the same variable
set by hand, so an earlier session hit this and did not write it down.

**Nothing in `tools/review-ledger.py` was edited and no existing signature was rewritten.** Whose read
it is when an agent does the reading is his call, and rewriting a record of who read what is the one
repair nobody can check afterwards. This session's own mark was made with the variable overridden.

> **A NEGATIVE RESULT FROM THE SAME READ, so nobody re-derives it.** The rest of
> `brain/heron_research.py` is sound and it is one of the better-argued files here: the no-network
> rule is enforced by a suite that greps this file rather than by a comment; there is deliberately no
> verdict above `UNVERIFIED` and a check exists to fail the day somebody adds one; a SKIPPED scope is
> a **prerequisite** and not a miss, because sending somebody to the internet for a clause sitting in
> their own project specification is the worst outcome the stage has; the brief offers every ingest
> destination and chooses none, because search order is not storage intent; `_NORMATIVE` is grammar
> rather than the domain word list R-60 forbids; and `_is_claim` **states its own limit** instead of
> widening until it flags everything.

#### And `brain/heron_conflict.py`, the next one down the same measurement

**Read end to end, 693 lines, and it is one of the best-argued files in `brain/`** — recorded here so
nobody reads it again. It **surfaces and never resolves** (R-24); the docs/20 §2 hierarchy is printed
and explicitly **not applied**; only a **number and a clause number** ever cross between scopes, each
store opened, reduced to values and closed before the next is opened; `quantity()` filters on one
shape rather than an exclusion list, **and says the list it replaced was dead code, checked by running
it**; `comparable()` converts mm to cm because 1 cm **is** 10 mm and deliberately holds no pair that
needs a judgement; `same_number` stops *30* and *30.0* reading as a disagreement; `source` is keyed on
the **document id** rather than the title; `says` compares **sets**, so a second value is not thrown
away before the comparison; and the no-disagreement message says plainly that it is **not the same as
agreement** (D-52).

**One thing in it is wrong and it is [row 5b-104](../FRAGMENT-ISSUES.md), OPEN and not fixed here.**
`main()` reads `argv[i + 1]` for `--scopes` and `--project` without asking whether a value was given,
so either flag last on the line answers a typo with **`IndexError: list index out of range`**.

**THE SCAN WAS WRONG ABOUT TWO OF THE SIX IT FOUND, so every one was RUN.** `grep` for `argv[i + 1]`
hits eight sites; `heron_ingest`, `heron_ground` and `check-products` all refuse properly. **And the
worst of the four is not a crash:** `brain/heron_company.py --subject` is guarded with
`and i + 1 < len(argv)`, so the flag falls through to `words.append` and joins the question — measured,
it searches for **`'how thick is duct insulation --subject'`** and exits **0**, which is word for word
what the comment three lines above it says must not happen. **The house answer already exists in the
same stage**: `heron_research._flag` refuses by name and exits 2.

#### `brain/heron_ingest.py` — read end to end, 1,785 lines, NOTHING FOUND

**The thinnest-held live-path module left** once `heron_research` and `heron_conflict` were done —
16 suites against 16 public functions, and never opened. **Recorded so nobody reads it again.**

**The two rules it is built around were MEASURED, not read.** R-68, a rule cut from its exception:
across a sixty-sentence body at limit 400, **no piece came back starting with a qualifier**. R-08, a
protected token cut in half: `OST_DuctCurves` and `QCS 2014 s21.3.2` both survive **whole** across
cuts at limit 300 and 280.

**Every CLI guard it claims was RUN and every one holds**: `--scop` refused before a file is opened,
`--project` without `--scope project` **refused rather than corrected**, `--scope` with no value
refused by name at exit 2, a `.rvt` refused on the extension before the file is opened, and a real
`.md` ingests cleanly. **That last one is the contrast worth keeping** — this is the module
[row 5b-104](../FRAGMENT-ISSUES.md) says the other four should copy, and its `_flag` is the shape they
are missing.

Worth knowing rather than re-deriving: `_cut` is iterative over a **window** with a 400-character
margin, because recursive died on `RecursionError` and whole-text was quadratic at 60 s per 1.6 MB;
`_split_points` returns the **rank** so a blank line past the limit cannot end the search before a
sentence end inside it; an unnumbered block gets `para-N` rather than an empty locator, because a
chunk that cannot be cited is R-21's bug; the manifest lives **beside** the store so deleting the
derived file stays a safe recovery action (GR 11); and a retired revision comes back from it as a
**row**, never as invented text.

#### `brain/heron_matrix.py` — the matrix says nothing is proven, and 328 proofs say otherwise

**[Row 5b-105](../FRAGMENT-ISSUES.md). OPEN, measured, and not fixed here.** Next down the same
measurement: 338 lines, 7 public functions, 13 suites, never opened.

**Run it on this checkout and every cell of the PROVEN column is zero, on all eight releases.**
`build_matrix()` over the real library finds **328 fragments whose proof names exactly one Revit
release** — 315 on 2024, 13 on 2020. The line is

```python
if proven_on == rel and state == COMPILES:
    state = PROVEN
```

and `COMPILES` needs `build/compile-results.json`, which `.gitignore` excludes **twice**. So on a
fresh checkout **PROVEN is unreachable**, and `docs/28`'s *"status coming from TESTS, NEVER
ASSUMPTION"* reports zero tests.

**The file states the rule that would have caught it, and nothing uses it.**
`STRENGTH = [NOT_CLAIMED, UNKNOWN, CLAIMED, COMPILES, PROVEN]`, commented *"A cell only ever moves up
this list"* — **referenced nowhere in the repository.** A prerequisite chain is not an ordering.
`UNKNOWN` is the same thing one size down: `build_matrix` never assigns it.

**And the proof is the stronger evidence.** A D-30 proof is a recorded run against a named real model
**on that release**, which cannot happen unless it compiled there — so the gate makes the weaker
evidence a precondition for the stronger.

**WHY THE SUITE PASSES, and it is the session's fourth instance of one shape.**
`tests/test_matrix.py` §5 checks the promotion **after injecting a fake `evidence` dict**; §3 checks
the no-evidence path with a library holding **no PROVEN fragment**. Both halves are tested and the
case a person actually runs falls between them — the same blind spot as
[row 5b-102](../FRAGMENT-ISSUES.md), where every test input put the document first.

> **Left OPEN rather than fixed for one reason only**: whether the gate is intended is a judgement,
> and no sentence anywhere argues that a proof needs a compiler's agreement. If it is not intended,
> the repair is that one line plus a §5 check that a PROVEN fragment with **no** evidence still reads
> PROVEN on the release its proof names.
