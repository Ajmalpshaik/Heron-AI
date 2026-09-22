# Session note — EVERY LIVE-PATH BRAIN MODULE HAS NOW BEEN OPENED

> **Archived session note** from 2026-09-22. It was moved out of [`HANDOVER.md`](../HANDOVER.md) on
> 2026-09-23 by [`tools/archive-handover.py`](../../tools/archive-handover.py), so that file could go back to
> being the short live entry point it is supposed to be. **Its words are unchanged; only its links were
> re-pointed.** Nothing here is specification: where it disagrees with [DECISIONS.md](../DECISIONS.md),
> the [Golden Rules](../14-golden-rules.md) or the [Constitution](../../HERON_CONSTITUTION.md), **those
> win**. A note records what was true on its own day.

---

### 2026-09-22 — EVERY LIVE-PATH BRAIN MODULE HAS NOW BEEN OPENED

**Derive it, do not read it here:** `python tools/review-ledger.py --next 120` and look for the
seventeen. As this was written the queue listed **two** of them — `heron_ground` and `heron_retrieve`,
both PART — and **none unopened**.

The set is the one [row 5b-101](../FRAGMENT-ISSUES.md)'s method names: every module reachable from `mcp/`
with public module-level functions, classes excluded. `brain/heron_fragment.py` was the last one
nobody had opened — **1,153 lines, 15 public functions, 106 suites, the thickest-held of the
seventeen** — and it is **clean**.

| | |
|---|---|
| read in full | **15** |
| read in part | **2** — `heron_ground`, `heron_retrieve` |
| never opened | **0** |

**That does not mean `brain/` is read.** It is 53 modules; the other 36 are not on the path a
modeller's request takes, which is the distinction the last session's sweep turned on and the reason
this subset was worth finishing first.

#### `brain/heron_fragment.py` — and a near miss dismissed, which is the fourth this session

`can_promote()` guards its **target** argument and not the fragment's own status, so
`STATUSES.index(frag.status)` raises `ValueError` on a status that is not a lifecycle state — a
contract violation in a function whose docstring promises `(allowed, reason)`.

**It is a shape and not a behaviour, and no row was written.** `tools/check-signatures.py` is the only
gate that calls it, and it **skips every fragment whose status is not `DRAFT` before the call** — so
`can_promote` is only ever handed a valid one. An empty or `None` status falls back to `DISCOVERED`
and returns a pair correctly, and `validate()` catches an invalid status with a good message on the
path that actually runs.

> **Four times this session a measurement stopped a row being written**: `heron_search`'s four
> functions named by no suite (all reached internally), `heron_search`'s two defaulted arguments (both
> passed at every call site), the `argv[i + 1]` scan that was wrong about two of six, and this. **Shape
> is not behaviour**, and the cost of checking is minutes against a row somebody has to disbelieve
> later.

The library validates clean today — **396 well-formed, 328 PROVEN, 68 DRAFT, exit 0** — and the two
listed `STALE` are DRAFT signatures, which is `check-signatures`' business rather than a `validate()`
problem.
