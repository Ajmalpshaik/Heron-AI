# Session note — (closing) — SIX PROMISES NOBODY WROTE DOWN, AND A SWEEP THAT WAS WRONG 28 TIMES

> **Archived session note** from 2026-09-21. It was moved out of [`HANDOVER.md`](../HANDOVER.md) on
> 2026-09-23 by [`tools/archive-handover.py`](../../tools/archive-handover.py), so that file could go back to
> being the short live entry point it is supposed to be. **Its words are unchanged; only its links were
> re-pointed.** Nothing here is specification: where it disagrees with [DECISIONS.md](../DECISIONS.md),
> the [Golden Rules](../14-golden-rules.md) or the [Constitution](../../HERON_CONSTITUTION.md), **those
> win**. A note records what was true on its own day.

---

### 2026-09-21 (closing) — SIX PROMISES NOBODY WROTE DOWN, AND A SWEEP THAT WAS WRONG 28 TIMES

**[Row 5b-95](../FRAGMENT-ISSUES.md). OPEN — for a sitting, and the question is one sentence long.**

**Six refusals the code produces and no contract declares**, across four agents:
`HERON-FRG-UPD-008` → `STALE_APPROVAL`, `HERON-INS-HLT-009` → `NOT_CHECKED_BY_A_CHECK`,
`HERON-NAM-VAL-002` → `MISSING_PART`, `BAD_VERSION`, `GENERATED_A_BAD_NAME`, and
`HERON-SKL-UPD-003` → `REVISION_INCOMPLETE`.

**`tools/generate-contract-reference.py` already reports them on every run and nothing records them**,
so they are re-discovered and never closed. That is the whole reason the row exists. In the tool's own
words this is *"the direction that breaks at run time"*: a caller handling every declared failure
still meets an unhandled one.

**Every one was read and every one is a correct refusal.** The three in `heron_naming` are all from
`generate()`, whose eight declared failures are all from the `check()` side — the generate half was
added and the contract never followed. **The session's own shape a fifth time.**

**WHY IT IS OPEN.** `heron_contract.compare()` calls **an added failure state BREAKING**, which is
MAJOR — so declaring these six takes four contracts from `1.0.0` to `2.0.0` **for a documentation
correction, with no agent behaving any differently afterwards**. `compare()` cannot tell *the promise
changed* from *the promise was written down wrong*, and all six sit in that gap. The alternative,
making the code return an already-declared refusal, was checked and is wrong for all six: no declared
code fits.

**It is not urgent and the row says so.** `compare()` is exercised by its own suite and nothing else;
no gate compares a contract against its previous version. The consequence today is a rule, not a red
build.

---

**AND A NEGATIVE SWEEP WORTH MORE THAN THE ROW.** Before finding the tool, I measured the same
question by hand and it was **wrong 28 times out of 28**. My scan said 26 contracts declare a refusal
the code never names; the tool says **0**, and the tool is right — those codes are named as the prefix
of an exception message (`raise LookupError("NO_SUCH_HANDLE: ...")`), and two more of my hits were a
per-item annotation inside a list (`excluded.append({"refused": "WRONG_REVIT_VERSION"})`) mistaken for
the agent's own refusal. **The tool recognises a refusal by POSITION**, which is the first line of its
own suite.

**Third time in one day a crude scan was wrong about nearly everything it flagged** —
[5b-86](../FRAGMENT-ISSUES.md) at 15 of 16, [5b-92](../FRAGMENT-ISSUES.md) at 6 of 6, this at 28 of 28.
`AGENTS.md` already says the thing that would have saved all three: **check whether a tool already
owns the answer. Tool output beats a typed sentence, always.**

**Before measuring a class by hand, run this and see whether it is already answered:**

```bash
ls tools/*.py | head -50          # 47 of them, and several ask questions like yours
python tools/generate-contract-reference.py
python tools/module-reach.py
python tools/open-defects.py
```

**Also checked and discarded**: a scan of the seven ADMIN surfaces for the shapes that paid off
earlier today — a bare `max()`/`min()`, an unguarded `[0]`, an `int()` on caller input — found five
subscripts and **all five were guarded or inside a `main()` demonstration**. No row.
